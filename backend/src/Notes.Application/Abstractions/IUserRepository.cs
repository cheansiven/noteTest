using Notes.Domain.Users;

namespace Notes.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cheap existence probe used when validating a bearer token: a JWT stays
    /// cryptographically valid until it expires, even if the account behind it is gone.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
