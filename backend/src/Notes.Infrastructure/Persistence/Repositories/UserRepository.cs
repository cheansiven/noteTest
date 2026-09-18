using Dapper;
using Notes.Application.Abstractions;
using Notes.Domain.Users;

namespace Notes.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    /// <summary>Shape returned by SELECT; translated into the domain model below.</summary>
    private sealed record UserRow(Guid Id, string Email, string DisplayName, string PasswordHash, DateTime CreatedAt);

    private const string SelectColumns = "Id, Email, DisplayName, PasswordHash, CreatedAt";

    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM dbo.Users WHERE Email = @Email;",
            new { Email = email.Value },
            cancellationToken: cancellationToken));

        return row is null ? null : ToDomain(row);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM dbo.Users WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));

        return row is null ? null : ToDomain(row);
    }

    public async Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var found = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT 1 FROM dbo.Users WHERE Email = @Email;",
            new { Email = email.Value },
            cancellationToken: cancellationToken));

        return found is not null;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var found = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT 1 FROM dbo.Users WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));

        return found is not null;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Users (Id, Email, DisplayName, PasswordHash, CreatedAt)
            VALUES (@Id, @Email, @DisplayName, @PasswordHash, @CreatedAt);
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                user.Id,
                Email = user.Email.Value,
                DisplayName = user.DisplayName.Value,
                PasswordHash = user.PasswordHash.Value,
                user.CreatedAt,
            },
            cancellationToken: cancellationToken));
    }

    private static User ToDomain(UserRow row) =>
        User.FromPersistence(row.Id, row.Email, row.DisplayName, row.PasswordHash, row.CreatedAt);
}
