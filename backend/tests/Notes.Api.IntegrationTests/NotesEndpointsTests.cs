using System.Net;
using System.Net.Http.Json;
using Notes.Application.Contracts;

namespace Notes.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class NotesEndpointsTests : IAsyncLifetime
{
    private readonly NotesApiFactory _factory;
    private ApiClient _anonymous = null!;
    private ApiClient _alice = null!;

    public NotesEndpointsTests(NotesApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.ResetAsync();
        _anonymous = _factory.CreateApiClient();
        var auth = await _anonymous.RegisterOkAsync($"alice-{Guid.NewGuid():N}@example.com");
        _alice = _factory.CreateApiClient(auth.Token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /* ----------------------------------------------------------------- access -- */

    [Fact]
    public async Task Every_notes_endpoint_requires_authentication()
    {
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await _anonymous.ListNotesRawAsync()).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _anonymous.GetNoteAsync(id)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _anonymous.CreateNoteAsync("Title")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _anonymous.UpdateNoteAsync(id, "Title", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _anonymous.DeleteNoteAsync(id)).StatusCode);
    }

    /* ------------------------------------------------------------------- crud -- */

    [Fact]
    public async Task Create_returns_201_with_a_location_header_that_resolves()
    {
        var response = await _alice.CreateNoteAsync("Shopping list", "Milk and eggs");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var followed = await _alice.Raw.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, followed.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_a_blank_title_with_400()
    {
        var response = await _alice.CreateNoteAsync("   ", "body");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Whitespace_only_content_is_stored_as_null()
    {
        var note = await _alice.CreateNoteOkAsync("Empty note", "   ");

        Assert.Null(note.Content);

        var listed = await _alice.ListNotesAsync();
        Assert.False(listed.Items.Single().HasContent);
    }

    [Fact]
    public async Task Update_changes_the_note_and_moves_UpdatedAt_only()
    {
        var note = await _alice.CreateNoteOkAsync("Draft", "First version");

        // CreatedAt/UpdatedAt are DATETIME2(3), so a same-millisecond edit would be
        // indistinguishable. Exact advancement is asserted in NoteServiceTests with a
        // controllable clock; here we just let the real clock tick.
        await Task.Delay(25);

        var response = await _alice.UpdateNoteAsync(note.Id, "Final", "Second version");
        var updated = await response.Content.ReadFromJsonAsync<NoteDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Final", updated!.Title);
        Assert.Equal(note.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > note.UpdatedAt);
    }

    [Fact]
    public async Task Delete_removes_the_note_and_a_second_delete_returns_404()
    {
        var note = await _alice.CreateNoteOkAsync("Temporary");

        Assert.Equal(HttpStatusCode.NoContent, (await _alice.DeleteNoteAsync(note.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _alice.DeleteNoteAsync(note.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _alice.GetNoteAsync(note.Id)).StatusCode);
    }

    /* -------------------------------------------------------------- isolation -- */

    [Fact]
    public async Task A_user_can_never_reach_another_users_note()
    {
        var aliceNote = await _alice.CreateNoteOkAsync("Alice private", "secret");

        var bobAuth = await _anonymous.RegisterOkAsync($"bob-{Guid.NewGuid():N}@example.com");
        var bob = _factory.CreateApiClient(bobAuth.Token);

        // 404 rather than 403: telling Bob "forbidden" would confirm the id exists.
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetNoteAsync(aliceNote.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.UpdateNoteAsync(aliceNote.Id, "hacked", "x")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteNoteAsync(aliceNote.Id)).StatusCode);

        Assert.Empty((await bob.ListNotesAsync()).Items);

        // Alice's note survived all of it, unchanged.
        var stillThere = await (await _alice.GetNoteAsync(aliceNote.Id)).Content.ReadFromJsonAsync<NoteDto>();
        Assert.Equal("Alice private", stillThere!.Title);
        Assert.Equal("secret", stillThere.Content);
    }

    /* ------------------------------------------------- search / filter / sort -- */

    [Fact]
    public async Task Search_matches_title_and_content_case_insensitively()
    {
        await _alice.CreateNoteOkAsync("Groceries", "Milk and eggs");
        await _alice.CreateNoteOkAsync("Standup", "Shipped the DAPPER layer");

        Assert.Equal("Groceries", (await _alice.ListNotesAsync("?search=groc")).Items.Single().Title);
        Assert.Equal("Standup", (await _alice.ListNotesAsync("?search=dapper")).Items.Single().Title);
    }

    [Fact]
    public async Task Search_treats_SQL_LIKE_wildcards_as_literal_text()
    {
        await _alice.CreateNoteOkAsync("Discount", "50% off coffee");
        await _alice.CreateNoteOkAsync("Plain", "nothing special");

        // Unescaped, "%" would match every row.
        var percent = await _alice.ListNotesAsync("?search=50%25");
        Assert.Equal("Discount", Assert.Single(percent.Items).Title);

        // Likewise "_" is a single-character wildcard in LIKE.
        Assert.Empty((await _alice.ListNotesAsync("?search=5_%25")).Items);
    }

    [Fact]
    public async Task Notes_can_be_sorted_by_title_in_both_directions()
    {
        await _alice.CreateNoteOkAsync("Banana");
        await _alice.CreateNoteOkAsync("Apple");
        await _alice.CreateNoteOkAsync("Cherry");

        var ascending = await _alice.ListNotesAsync("?sortBy=Title&sortDirection=Asc");
        var descending = await _alice.ListNotesAsync("?sortBy=Title&sortDirection=Desc");

        Assert.Equal(["Apple", "Banana", "Cherry"], ascending.Items.Select(i => i.Title));
        Assert.Equal(["Cherry", "Banana", "Apple"], descending.Items.Select(i => i.Title));
    }

    [Fact]
    public async Task Notes_can_be_filtered_by_whether_they_have_content()
    {
        await _alice.CreateNoteOkAsync("With body", "something");
        await _alice.CreateNoteOkAsync("Without body");

        Assert.Equal("With body", (await _alice.ListNotesAsync("?hasContent=true")).Items.Single().Title);
        Assert.Equal("Without body", (await _alice.ListNotesAsync("?hasContent=false")).Items.Single().Title);
    }

    [Fact]
    public async Task Paging_reports_totals_and_never_repeats_a_note()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _alice.CreateNoteOkAsync($"Note {i}");
        }

        var first = await _alice.ListNotesAsync("?pageSize=2&page=1");
        var second = await _alice.ListNotesAsync("?pageSize=2&page=2");
        var third = await _alice.ListNotesAsync("?pageSize=2&page=3");

        Assert.Equal(5, first.TotalCount);
        Assert.Equal(3, first.TotalPages);
        Assert.True(first.HasNext);
        Assert.False(first.HasPrevious);
        Assert.False(third.HasNext);

        var seen = first.Items.Concat(second.Items).Concat(third.Items).Select(i => i.Id).ToList();
        Assert.Equal(5, seen.Distinct().Count());
    }

    [Fact]
    public async Task An_oversized_page_size_is_clamped_rather_than_rejected()
    {
        await _alice.CreateNoteOkAsync("Only note");

        var page = await _alice.ListNotesAsync("?pageSize=100000");

        Assert.Equal(NoteQuery.DefaultPageSize, page.PageSize);
    }

    [Fact]
    public async Task The_list_ships_a_preview_rather_than_the_whole_body()
    {
        var body = new string('x', 5_000);
        await _alice.CreateNoteOkAsync("Long note", body);

        var item = (await _alice.ListNotesAsync()).Items.Single();

        Assert.True(item.HasContent);
        Assert.NotNull(item.Preview);
        Assert.True(item.Preview!.Length < body.Length);
    }

    /* ----------------------------------------------------------------- format -- */

    [Fact]
    public async Task A_timestamp_survives_the_round_trip_to_SQL_Server_unchanged()
    {
        // Regression guard: Dapper's default DateTime mapping uses the legacy `datetime`
        // type, whose ~3.33ms resolution silently shifted values by a millisecond or two.
        var created = await _alice.CreateNoteOkAsync("Precise");

        var reread = await (await _alice.GetNoteAsync(created.Id)).Content.ReadFromJsonAsync<NoteDto>();

        Assert.Equal(created.CreatedAt, reread!.CreatedAt);
        Assert.Equal(created.UpdatedAt, reread.UpdatedAt);
    }

    [Fact]
    public async Task Timestamps_are_serialised_as_explicit_UTC()
    {
        await _alice.CreateNoteOkAsync("Timestamped");

        var json = await (await _alice.ListNotesRawAsync()).Content.ReadAsStringAsync();

        // Without the trailing Z a browser would read these as local time.
        Assert.Contains("\"createdAt\":\"", json);
        Assert.Matches(@"""createdAt"":""\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z""", json);
    }
}
