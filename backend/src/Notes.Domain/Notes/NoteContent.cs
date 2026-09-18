using Notes.Domain.Common;

namespace Notes.Domain.Notes;

/// <summary>
/// Optional note body. Whitespace-only input collapses to "empty", so the database
/// never holds a blank string that behaves differently from NULL.
/// </summary>
public sealed class NoteContent : ValueObject
{
    public const int MaxLength = 20_000;

    public static readonly NoteContent Empty = new(null);

    private NoteContent(string? value) => Value = value;

    public string? Value { get; }

    public bool HasValue => Value is not null;

    public static Result<NoteContent> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result.Success(Empty);
        }

        var candidate = input.Trim();

        return candidate.Length > MaxLength
            ? Result.Failure<NoteContent>(NoteErrors.ContentTooLong(MaxLength))
            : Result.Success(new NoteContent(candidate));
    }

    public static NoteContent FromPersistence(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Empty : new NoteContent(value);

    public override string ToString() => Value ?? string.Empty;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
