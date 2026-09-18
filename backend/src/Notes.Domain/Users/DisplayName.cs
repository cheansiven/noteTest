using Notes.Domain.Common;

namespace Notes.Domain.Users;

public sealed class DisplayName : ValueObject
{
    public const int MinLength = 2;
    public const int MaxLength = 100;

    private DisplayName(string value) => Value = value;

    public string Value { get; }

    public static Result<DisplayName> Create(string? input)
    {
        var candidate = input?.Trim();

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Result.Failure<DisplayName>(UserErrors.DisplayNameRequired);
        }

        return candidate.Length is < MinLength or > MaxLength
            ? Result.Failure<DisplayName>(UserErrors.DisplayNameLength(MinLength, MaxLength))
            : Result.Success(new DisplayName(candidate));
    }

    public static DisplayName FromPersistence(string value) => new(value);

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
