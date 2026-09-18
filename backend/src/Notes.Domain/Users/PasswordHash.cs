using Notes.Domain.Common;

namespace Notes.Domain.Users;

/// <summary>
/// A hashed password. Typed separately from <see cref="Password"/> so a plaintext
/// value can never be assigned where a hash is expected.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    public const int MaxLength = 256;

    private PasswordHash(string value) => Value = value;

    public string Value { get; }

    public static PasswordHash FromHash(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Password hash cannot be empty.", nameof(value))
            : new PasswordHash(value);

    public override string ToString() => "********";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
