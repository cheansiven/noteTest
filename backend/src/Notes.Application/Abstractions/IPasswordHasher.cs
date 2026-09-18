using Notes.Domain.Users;

namespace Notes.Application.Abstractions;

/// <summary>
/// Keeps the hashing algorithm out of the use cases, so BCrypt could be swapped for
/// Argon2 without touching a single service.
/// </summary>
public interface IPasswordHasher
{
    PasswordHash Hash(Password password);

    bool Verify(Password password, PasswordHash hash);
}
