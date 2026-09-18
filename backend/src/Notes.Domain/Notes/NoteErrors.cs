using Notes.Domain.Common;

namespace Notes.Domain.Notes;

public static class NoteErrors
{
    public static readonly Error TitleRequired =
        Error.Validation("Note.Title.Required", "Title is required.");

    public static Error TitleTooLong(int max) =>
        Error.Validation("Note.Title.TooLong", $"Title must be {max} characters or fewer.");

    public static Error ContentTooLong(int max) =>
        Error.Validation("Note.Content.TooLong", $"Content must be {max} characters or fewer.");

    /// <summary>
    /// Also returned when the note belongs to somebody else: telling the caller
    /// "forbidden" would confirm that the id exists.
    /// </summary>
    public static Error NotFound(Guid id) =>
        Error.NotFound("Note.NotFound", $"Note '{id}' was not found.");
}
