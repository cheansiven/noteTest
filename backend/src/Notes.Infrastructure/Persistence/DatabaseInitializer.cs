using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Notes.Infrastructure.Persistence;

/// <summary>
/// Creates the database if it is missing and applies the schema at start-up, so a
/// reviewer only needs a reachable SQL Server - there is no manual migration step.
/// <para>
/// The script is compiled into the assembly as an embedded resource, so it cannot go
/// missing in a published or containerised build.
/// </para>
/// </summary>
public sealed class DatabaseInitializer
{
    private const string SchemaResourceName = "Notes.Infrastructure.schema.sql";

    /// <summary>
    /// SQL Server accepts TCP connections shortly before it accepts logins, and under
    /// Docker both containers start together, so the first attempts can legitimately fail.
    /// </summary>
    private const int MaxAttempts = 12;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly string _connectionString;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(string connectionString, ILogger<DatabaseInitializer> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("The connection string must specify a database.");
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await EnsureDatabaseExistsAsync(builder, databaseName, cancellationToken);
                await ApplySchemaAsync(cancellationToken);

                _logger.LogInformation("Database '{Database}' is ready.", databaseName);
                return;
            }
            catch (SqlException ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(
                    "SQL Server is not ready yet (attempt {Attempt}/{MaxAttempts}): {Message}. Retrying in {Delay}s.",
                    attempt, MaxAttempts, ex.Message, RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    private async Task EnsureDatabaseExistsAsync(
        SqlConnectionStringBuilder builder, string databaseName, CancellationToken cancellationToken)
    {
        // Connect to master, because the target database may not exist yet.
        var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString) { InitialCatalog = "master" };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var exists = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT 1 FROM sys.databases WHERE name = @Name;",
            new { Name = databaseName },
            cancellationToken: cancellationToken));

        if (exists is not null)
        {
            return;
        }

        // A database name cannot be parameterised; escaping the delimiter keeps it safe.
        var escaped = databaseName.Replace("]", "]]");
        await connection.ExecuteAsync(new CommandDefinition(
            $"CREATE DATABASE [{escaped}];", cancellationToken: cancellationToken));

        _logger.LogInformation("Created database '{Database}'.", databaseName);
    }

    private async Task ApplySchemaAsync(CancellationToken cancellationToken)
    {
        var script = await ReadSchemaScriptAsync(cancellationToken);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(script, cancellationToken: cancellationToken));
    }

    private static async Task<string> ReadSchemaScriptAsync(CancellationToken cancellationToken)
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SchemaResourceName)
            ?? throw new InvalidOperationException($"Embedded schema '{SchemaResourceName}' was not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
