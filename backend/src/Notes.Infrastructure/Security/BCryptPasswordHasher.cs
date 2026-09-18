using Notes.Application.Abstractions;
using Notes.Domain.Users;

namespace Notes.Infrastructure.Security;

/// <summary>
/// BCrypt with a per-password salt and a configurable work factor. The factor is stored
/// inside the hash, so raising it later still verifies existing passwords.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public PasswordHash Hash(Password password) =>
        PasswordHash.FromHash(BCrypt.Net.BCrypt.HashPassword(password.Value, WorkFactor));

    public bool Verify(Password password, PasswordHash hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password.Value, hash.Value);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A malformed stored hash must read as "wrong password", never as a crash.
            return false;
        }
    }
}
