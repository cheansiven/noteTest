using System.Net.Mail;
using Notes.Domain.Common;

namespace Notes.Domain.Users;

/// <summary>
/// A validated, normalised email address. Once constructed it cannot be invalid,
/// so no other layer needs to re-check it.
/// </summary>
public sealed class Email : ValueObject
{
    public const int MaxLength = 256;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? input)
    {
        var candidate = input?.Trim();

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Result.Failure<Email>(UserErrors.EmailRequired);
        }

        if (candidate.Length > MaxLength)
        {
            return Result.Failure<Email>(UserErrors.EmailTooLong(MaxLength));
        }

        // MailAddress is stricter and better tested than a hand-rolled regex.
        if (!MailAddress.TryCreate(candidate, out var parsed) || parsed.Address != candidate)
        {
            return Result.Failure<Email>(UserErrors.EmailInvalid);
        }

        // Stored lower-cased so lookups are unambiguous and the unique index holds.
        return Result.Success(new Email(candidate.ToLowerInvariant()));
    }

    /// <summary>Rebuilds a value already persisted, bypassing validation.</summary>
    public static Email FromPersistence(string value) => new(value);

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
