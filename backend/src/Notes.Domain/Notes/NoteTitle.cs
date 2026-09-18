using Notes.Domain.Common;

namespace Notes.Domain.Notes;

public sealed class NoteTitle : ValueObject
{
    public const int MaxLength = 200;

    private NoteTitle(string value) => Value = value;

    public string Value { get; }

    public static Result<NoteTitle> Create(string? input)
    {
        var candidate = input?.Trim();

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Result.Failure<NoteTitle>(NoteErrors.TitleRequired);
        }

        return candidate.Length > MaxLength
            ? Result.Failure<NoteTitle>(NoteErrors.TitleTooLong(MaxLength))
            : Result.Success(new NoteTitle(candidate));
    }

    public static NoteTitle FromPersistence(string value) => new(value);

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
