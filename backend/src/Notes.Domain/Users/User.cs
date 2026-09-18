using Notes.Domain.Common;

namespace Notes.Domain.Users;

/// <summary>
/// A registered account. State is private-set and only changes through the methods
/// below, so an instance can never drift into an invalid shape.
/// </summary>
public sealed class User : Entity<Guid>
{
    private User(Guid id, Email email, DisplayName displayName, PasswordHash passwordHash, DateTime createdAt)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public Email Email { get; }

    public DisplayName DisplayName { get; private set; }

    public PasswordHash PasswordHash { get; private set; }

    public DateTime CreatedAt { get; }

    /// <summary>Creates a brand-new account. The caller supplies an already-hashed password.</summary>
    public static User Register(Email email, DisplayName displayName, PasswordHash passwordHash, DateTime utcNow) =>
        new(Guid.NewGuid(), email, displayName, passwordHash, utcNow);

    public void Rename(DisplayName displayName) => DisplayName = displayName;

    public void ChangePassword(PasswordHash passwordHash) => PasswordHash = passwordHash;

    /// <summary>
    /// Rebuilds an instance from a stored row. Separate from <see cref="Register"/> so that
    /// loading never re-runs creation rules or invents a new id.
    /// </summary>
    public static User FromPersistence(
        Guid id, string email, string displayName, string passwordHash, DateTime createdAt) =>
        new(id,
            Email.FromPersistence(email),
            DisplayName.FromPersistence(displayName),
            PasswordHash.FromHash(passwordHash),
            createdAt);
}
