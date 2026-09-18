using NSubstitute;
using Notes.Application.Abstractions;
using Notes.Application.Contracts;
using Notes.Application.Notes;
using Notes.Application.UnitTests.Doubles;
using Notes.Domain.Common;
using Notes.Domain.Notes;

namespace Notes.Application.UnitTests.Notes;

public sealed class NoteServiceTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid NoteId = Guid.NewGuid();

    private readonly INoteRepository _repository = Substitute.For<INoteRepository>();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly NoteService _sut;

    public NoteServiceTests() => _sut = new NoteService(_repository, _clock);

    private Note ExistingNote(string title = "Shopping list", string? content = "Milk and eggs") =>
        Note.Create(Owner, NoteTitle.Create(title).Value, NoteContent.Create(content).Value, _clock.UtcNow);

    /* ------------------------------------------------------------------ create -- */

    [Fact]
    public async Task Create_saves_the_note_with_server_assigned_timestamps()
    {
        var result = await _sut.CreateNoteAsync(Owner, new CreateNoteRequest("Groceries", "Milk"));

        Assert.True(result.IsSuccess);
        Assert.Equal(_clock.UtcNow, result.Value.CreatedAt);
        Assert.Equal(_clock.UtcNow, result.Value.UpdatedAt);
        await _repository.Received(1).AddAsync(
            Arg.Is<Note>(n => n.OwnerId == Owner && n.Title.Value == "Groceries"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_rejects_a_blank_title_without_touching_the_repository()
    {
        var result = await _sut.CreateNoteAsync(Owner, new CreateNoteRequest("   ", "Milk"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_stores_whitespace_only_content_as_empty()
    {
        var result = await _sut.CreateNoteAsync(Owner, new CreateNoteRequest("Title", "   "));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Content);
    }

    /* -------------------------------------------------------------------- read -- */

    [Fact]
    public async Task Get_returns_not_found_when_the_note_is_missing_or_owned_by_someone_else()
    {
        // The repository is always queried with the caller's id, so "belongs to another
        // user" and "does not exist" are indistinguishable here - by design.
        _repository.GetAsync(Owner, NoteId).Returns((Note?)null);

        var result = await _sut.GetNoteAsync(Owner, NoteId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task List_scopes_the_query_to_the_caller()
    {
        _repository.SearchAsync(Owner, Arg.Any<NoteQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<NoteListItemDto>([], 1, 20, 0));

        await _sut.GetNotesAsync(Owner, new NoteQuery());

        await _repository.Received(1).SearchAsync(Owner, Arg.Any<NoteQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_normalises_client_supplied_paging_and_date_bounds()
    {
        NoteQuery? captured = null;
        _repository.SearchAsync(Owner, Arg.Do<NoteQuery>(q => captured = q), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<NoteListItemDto>([], 1, 20, 0));

        await _sut.GetNotesAsync(Owner, new NoteQuery
        {
            Page = 0,
            PageSize = 5_000,
            Search = "  groceries  ",
            CreatedTo = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc),
        });

        Assert.NotNull(captured);
        Assert.Equal(1, captured!.Page);
        Assert.Equal(NoteQuery.DefaultPageSize, captured.PageSize);
        Assert.Equal("groceries", captured.Search);
        // A date-only upper bound must include everything recorded that day.
        Assert.Equal(new DateTime(2026, 9, 18, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999), captured.CreatedTo);
    }

    /* ------------------------------------------------------------------ update -- */

    [Fact]
    public async Task Update_refreshes_UpdatedAt_and_leaves_CreatedAt_alone()
    {
        var note = ExistingNote();
        _repository.GetAsync(Owner, NoteId).Returns(note);
        _repository.UpdateAsync(note, Arg.Any<CancellationToken>()).Returns(true);
        var createdAt = note.CreatedAt;
        _clock.Advance(TimeSpan.FromHours(2));

        var result = await _sut.UpdateNoteAsync(Owner, NoteId, new UpdateNoteRequest("New title", "New body"));

        Assert.True(result.IsSuccess);
        Assert.Equal(createdAt, result.Value.CreatedAt);
        Assert.Equal(_clock.UtcNow, result.Value.UpdatedAt);
    }

    [Fact]
    public async Task Update_of_an_unchanged_note_skips_the_write_and_keeps_UpdatedAt()
    {
        var note = ExistingNote();
        _repository.GetAsync(Owner, NoteId).Returns(note);
        var originalUpdatedAt = note.UpdatedAt;
        _clock.Advance(TimeSpan.FromHours(2));

        var result = await _sut.UpdateNoteAsync(
            Owner, NoteId, new UpdateNoteRequest("Shopping list", "Milk and eggs"));

        Assert.True(result.IsSuccess);
        Assert.Equal(originalUpdatedAt, result.Value.UpdatedAt);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_returns_not_found_for_a_note_the_caller_does_not_own()
    {
        _repository.GetAsync(Owner, NoteId).Returns((Note?)null);

        var result = await _sut.UpdateNoteAsync(Owner, NoteId, new UpdateNoteRequest("Title", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_returns_not_found_when_the_note_is_deleted_mid_request()
    {
        var note = ExistingNote();
        _repository.GetAsync(Owner, NoteId).Returns(note);
        _repository.UpdateAsync(note, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.UpdateNoteAsync(Owner, NoteId, new UpdateNoteRequest("Changed", "Changed"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Update_rejects_a_blank_title_before_loading_the_note()
    {
        var result = await _sut.UpdateNoteAsync(Owner, NoteId, new UpdateNoteRequest("", "body"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _repository.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /* ------------------------------------------------------------------ delete -- */

    [Fact]
    public async Task Delete_succeeds_when_a_row_was_removed()
    {
        _repository.DeleteAsync(Owner, NoteId).Returns(true);

        Assert.True((await _sut.DeleteNoteAsync(Owner, NoteId)).IsSuccess);
    }

    [Fact]
    public async Task Delete_returns_not_found_when_nothing_matched()
    {
        _repository.DeleteAsync(Owner, NoteId).Returns(false);

        var result = await _sut.DeleteNoteAsync(Owner, NoteId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
