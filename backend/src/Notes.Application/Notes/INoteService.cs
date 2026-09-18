using Notes.Application.Contracts;
using Notes.Domain.Common;

namespace Notes.Application.Notes;

public interface INoteService
{
    Task<Result<PagedResult<NoteListItemDto>>> GetNotesAsync(
        Guid ownerId, NoteQuery query, CancellationToken cancellationToken = default);

    Task<Result<NoteDto>> GetNoteAsync(Guid ownerId, Guid noteId, CancellationToken cancellationToken = default);

    Task<Result<NoteDto>> CreateNoteAsync(
        Guid ownerId, CreateNoteRequest request, CancellationToken cancellationToken = default);

    Task<Result<NoteDto>> UpdateNoteAsync(
        Guid ownerId, Guid noteId, UpdateNoteRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteNoteAsync(Guid ownerId, Guid noteId, CancellationToken cancellationToken = default);
}
