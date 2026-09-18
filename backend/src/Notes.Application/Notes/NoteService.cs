using Notes.Application.Abstractions;
using Notes.Application.Contracts;
using Notes.Domain.Common;
using Notes.Domain.Notes;

namespace Notes.Application.Notes;

/// <summary>
/// Note use cases. Every method takes the owner id from the caller (the API reads it from
/// the bearer token), and every repository call is scoped by it, so one user's request can
/// never reach another user's row.
/// </summary>
public sealed class NoteService : INoteService
{
    private readonly INoteRepository _notes;
    private readonly IDateTimeProvider _clock;

    public NoteService(INoteRepository notes, IDateTimeProvider clock)
    {
        _notes = notes;
        _clock = clock;
    }

    public async Task<Result<PagedResult<NoteListItemDto>>> GetNotesAsync(
        Guid ownerId, NoteQuery query, CancellationToken cancellationToken = default)
    {
        var page = await _notes.SearchAsync(ownerId, query.Normalized(), cancellationToken);
        return Result.Success(page);
    }

    public async Task<Result<NoteDto>> GetNoteAsync(
        Guid ownerId, Guid noteId, CancellationToken cancellationToken = default)
    {
        var note = await _notes.GetAsync(ownerId, noteId, cancellationToken);

        return note is null
            ? Result.Failure<NoteDto>(NoteErrors.NotFound(noteId))
            : Result.Success(ToDto(note));
    }

    public async Task<Result<NoteDto>> CreateNoteAsync(
        Guid ownerId, CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        var title = NoteTitle.Create(request.Title);
        if (title.IsFailure) return Result.Failure<NoteDto>(title.Error);

        var content = NoteContent.Create(request.Content);
        if (content.IsFailure) return Result.Failure<NoteDto>(content.Error);

        var note = Note.Create(ownerId, title.Value, content.Value, _clock.UtcNow);

        await _notes.AddAsync(note, cancellationToken);

        return Result.Success(ToDto(note));
    }

    public async Task<Result<NoteDto>> UpdateNoteAsync(
        Guid ownerId, Guid noteId, UpdateNoteRequest request, CancellationToken cancellationToken = default)
    {
        var title = NoteTitle.Create(request.Title);
        if (title.IsFailure) return Result.Failure<NoteDto>(title.Error);

        var content = NoteContent.Create(request.Content);
        if (content.IsFailure) return Result.Failure<NoteDto>(content.Error);

        var note = await _notes.GetAsync(ownerId, noteId, cancellationToken);
        if (note is null)
        {
            return Result.Failure<NoteDto>(NoteErrors.NotFound(noteId));
        }

        // The entity decides whether this is a real change and moves UpdatedAt itself.
        // Saving an unchanged note is a no-op rather than a pointless timestamp bump.
        if (!note.Edit(title.Value, content.Value, _clock.UtcNow))
        {
            return Result.Success(ToDto(note));
        }

        if (!await _notes.UpdateAsync(note, cancellationToken))
        {
            // Deleted between the read and the write.
            return Result.Failure<NoteDto>(NoteErrors.NotFound(noteId));
        }

        return Result.Success(ToDto(note));
    }

    public async Task<Result> DeleteNoteAsync(
        Guid ownerId, Guid noteId, CancellationToken cancellationToken = default) =>
        await _notes.DeleteAsync(ownerId, noteId, cancellationToken)
            ? Result.Success()
            : Result.Failure(NoteErrors.NotFound(noteId));

    private static NoteDto ToDto(Note note) =>
        new(note.Id, note.Title.Value, note.Content.Value, note.CreatedAt, note.UpdatedAt);
}
