using Notes.Application.Contracts;
using Notes.Domain.Notes;

namespace Notes.Application.Abstractions;

public interface INoteRepository
{
    /// <summary>
    /// Read side: returns a projection rather than hydrated entities. Listing does not
    /// need behaviour, and shipping full note bodies for a grid of previews would be
    /// wasteful, so the query path deliberately bypasses the domain model.
    /// </summary>
    Task<PagedResult<NoteListItemDto>> SearchAsync(
        Guid ownerId, NoteQuery query, CancellationToken cancellationToken = default);

    /// <summary>Write side: hydrates the aggregate, already scoped to its owner.</summary>
    Task<Note?> GetAsync(Guid ownerId, Guid noteId, CancellationToken cancellationToken = default);

    Task AddAsync(Note note, CancellationToken cancellationToken = default);

    /// <summary>Returns false when no row matched - the note was deleted or is not owned.</summary>
    Task<bool> UpdateAsync(Note note, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid ownerId, Guid noteId, CancellationToken cancellationToken = default);
}
