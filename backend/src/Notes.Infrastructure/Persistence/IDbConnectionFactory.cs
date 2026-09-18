using System.Data;

namespace Notes.Infrastructure.Persistence;

/// <summary>
/// Hands out short-lived ADO.NET connections for Dapper, so repositories never hold
/// connection-string details of their own.
/// </summary>
public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
