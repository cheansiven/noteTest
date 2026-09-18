using Notes.Domain.Common;

namespace Notes.Domain.Users;

/// <summary>
/// A plaintext password that has passed the strength rules. It exists only long enough
/// to be hashed and is never persisted or logged.
/// </summary>
public sealed class Password : ValueObject
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    private Password(string value) => Value = value;

    public string Value { get; }

    public static Result<Password> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result.Failure<Password>(UserErrors.PasswordRequired);
        }

        return input.Length is < MinLength or > MaxLength
            ? Result.Failure<Password>(UserErrors.PasswordLength(MinLength, MaxLength))
            : Result.Success(new Password(input));
    }

    /// <summary>Never leak the secret through logging or a debugger view.</summary>
    public override string ToString() => "********";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
