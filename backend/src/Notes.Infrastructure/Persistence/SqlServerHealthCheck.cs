using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Notes.Infrastructure.Persistence;

/// <summary>Reports the API as unhealthy when its database is unreachable.</summary>
public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqlServerHealthCheck(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT 1;", cancellationToken: cancellationToken));

            return HealthCheckResult.Healthy("SQL Server is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server is unreachable.", ex);
        }
    }
}
