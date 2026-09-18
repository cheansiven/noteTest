namespace Notes.Application.Contracts;

public sealed record CreateNoteRequest(string Title, string? Content);

public sealed record UpdateNoteRequest(string Title, string? Content);

/// <summary>Full note, returned by the detail, create and update endpoints.</summary>
public sealed record NoteDto(
    Guid Id,
    string Title,
    string? Content,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// List projection. <paramref name="Preview"/> is a server-trimmed excerpt so a page of
/// long notes does not ship megabytes of body text to the browser.
/// </summary>
public sealed record NoteListItemDto(
    Guid Id,
    string Title,
    string? Preview,
    bool HasContent,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
