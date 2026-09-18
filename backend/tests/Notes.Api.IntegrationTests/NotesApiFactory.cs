using System.Data;
using System.Net.Http.Headers;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Notes.Api.IntegrationTests;

/// <summary>
/// Boots the real API in-process against a real SQL Server, so these tests exercise what
/// unit tests cannot: the SQL itself, model binding, the auth middleware and status codes.
/// Each run gets its own database, which is dropped afterwards.
/// </summary>
public sealed class NotesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string DatabaseName = $"NotesDb_IT_{Guid.NewGuid():N}";

    /// <summary>
    /// Points at the docker-compose SQL Server by default; override with
    /// NOTES_TEST_SQLSERVER to run against another instance.
    /// </summary>
    private static string ServerConnectionString =>
        Environment.GetEnvironmentVariable("NOTES_TEST_SQLSERVER")
        ?? "Server=localhost,1434;User Id=sa;Password=Your_strong_Passw0rd;TrustServerCertificate=True;Encrypt=False;";

    private static string TestConnectionString =>
        new SqlConnectionStringBuilder(ServerConnectionString) { InitialCatalog = DatabaseName }.ConnectionString;

    /// <summary>
    /// The API uses top-level statements, so its WebApplicationBuilder reads configuration
    /// while the entry point runs - before WebApplicationFactory's ConfigureAppConfiguration
    /// callbacks get a chance to contribute. Environment variables are part of the default
    /// configuration sources, so setting them here is what actually reaches the host.
    /// </summary>
    static NotesApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__NotesDb", TestConnectionString);
        // These tests deliberately hammer the auth endpoints; the limiter has its own
        // dedicated test rather than throttling everything else.
        Environment.SetEnvironmentVariable("RateLimiting__Authentication__PermitLimit", "10000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotesDb"] = TestConnectionString,
                ["RateLimiting:Authentication:PermitLimit"] = "10000",
            }));
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await EnsureServerReachableAsync();

        // Touching the client starts the host, which creates the database and applies
        // the schema through the application's own initializer - the same code path
        // production uses.
        _ = CreateClient();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        SqlConnection.ClearAllPools();
        await DropTestDatabaseAsync();
    }

    public ApiClient CreateApiClient() => new(CreateClient());

    /// <summary>A client whose every request carries the given user's bearer token.</summary>
    public ApiClient CreateApiClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new ApiClient(client);
    }

    public IDbConnection OpenConnection() => new SqlConnection(TestConnectionString);

    /// <summary>Removes all rows so each test starts from a known state.</summary>
    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(TestConnectionString);
        await connection.ExecuteAsync("DELETE FROM dbo.Notes; DELETE FROM dbo.Users;");
    }

    private static async Task EnsureServerReachableAsync()
    {
        try
        {
            await using var connection = new SqlConnection(ServerConnectionString);
            await connection.OpenAsync();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "The integration tests need a SQL Server instance. Start one with " +
                "`docker compose up -d sqlserver`, or point NOTES_TEST_SQLSERVER at your own.",
                ex);
        }
    }

    private static async Task DropTestDatabaseAsync()
    {
        try
        {
            await using var connection = new SqlConnection(ServerConnectionString);
            await connection.ExecuteAsync($"""
                IF DB_ID('{DatabaseName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{DatabaseName}];
                END
                """);
        }
        catch (SqlException)
        {
            // A leftover test database is untidy, not a test failure.
        }
    }
}

/// <summary>Shares one API host and database across every integration test class.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<NotesApiFactory>
{
    public const string Name = "Notes API";
}
