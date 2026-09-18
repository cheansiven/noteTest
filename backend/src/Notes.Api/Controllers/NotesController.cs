using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notes.Application.Contracts;
using Notes.Application.Notes;

namespace Notes.Api.Controllers;

/// <summary>
/// CRUD over the caller's own notes. Every action derives the owner from the bearer
/// token, so the client never supplies - and can never forge - a user id.
/// </summary>
[Route("api/notes")]
[Authorize]
public sealed class NotesController : ApiControllerBase
{
    private readonly INoteService _notes;

    public NotesController(INoteService notes) => _notes = notes;

    /// <summary>Lists the caller's notes with search, filtering, sorting and paging.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NoteListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotes([FromQuery] NoteQuery query, CancellationToken cancellationToken)
    {
        var result = await _notes.GetNotesAsync(CurrentUserId, query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Failure(result.Error);
    }

    /// <summary>Returns one note in full, including its content.</summary>
    [HttpGet("{id:guid}", Name = nameof(GetNote))]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNote(Guid id, CancellationToken cancellationToken)
    {
        var result = await _notes.GetNoteAsync(CurrentUserId, id, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Failure(result.Error);
    }

    /// <summary>Creates a note. CreatedAt and UpdatedAt are assigned by the server.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateNote(CreateNoteRequest request, CancellationToken cancellationToken)
    {
        var result = await _notes.CreateNoteAsync(CurrentUserId, request, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetNote), new { id = result.Value.Id }, result.Value)
            : Failure(result.Error);
    }

    /// <summary>Updates title and content, refreshing UpdatedAt when something actually changed.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNote(
        Guid id, UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        var result = await _notes.UpdateNoteAsync(CurrentUserId, id, request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Failure(result.Error);
    }

    /// <summary>Deletes a note the caller owns.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNote(Guid id, CancellationToken cancellationToken)
    {
        var result = await _notes.DeleteNoteAsync(CurrentUserId, id, cancellationToken);

        return result.IsSuccess ? NoContent() : Failure(result.Error);
    }
}
