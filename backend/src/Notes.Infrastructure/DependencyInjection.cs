using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notes.Application.Abstractions;
using Notes.Infrastructure.Persistence;
using Notes.Infrastructure.Persistence.Repositories;
using Notes.Infrastructure.Security;
using Notes.Infrastructure.Time;

namespace Notes.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "NotesDb";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        DapperConfiguration.Configure();

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.AddSingleton(provider => new DatabaseInitializer(
            connectionString, provider.GetRequiredService<ILogger<DatabaseInitializer>>()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenProvider, JwtTokenProvider>();

        // Misconfiguration fails at start-up with a clear message, not on the first
        // request with an obscure cryptographic error.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sql-server", tags: ["ready"]);

        return services;
    }
}
