using Notes.Domain.Notes;

namespace Notes.Domain.UnitTests.Notes;

public sealed class NoteTests
{
    private static readonly DateTime CreatedOn = new(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EditedOn = new(2026, 9, 18, 11, 30, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.NewGuid();

    private static Note NewNote(string title = "Shopping list", string? content = "Milk and eggs") =>
        Note.Create(Owner, NoteTitle.Create(title).Value, NoteContent.Create(content).Value, CreatedOn);

    [Fact]
    public void Create_sets_both_timestamps_to_the_creation_time()
    {
        var note = NewNote();

        Assert.Equal(CreatedOn, note.CreatedAt);
        Assert.Equal(CreatedOn, note.UpdatedAt);
        Assert.True(note.IsUnedited);
    }

    [Fact]
    public void Create_requires_an_owner()
    {
        var exception = Assert.Throws<ArgumentException>(() => Note.Create(
            Guid.Empty, NoteTitle.Create("Title").Value, NoteContent.Empty, CreatedOn));

        Assert.Equal("ownerId", exception.ParamName);
    }

    [Fact]
    public void Edit_moves_UpdatedAt_but_never_CreatedAt()
    {
        var note = NewNote();

        var changed = note.Edit(
            NoteTitle.Create("Shopping list (revised)").Value,
            NoteContent.Create("Milk, eggs and coffee").Value,
            EditedOn);

        Assert.True(changed);
        Assert.Equal(CreatedOn, note.CreatedAt);
        Assert.Equal(EditedOn, note.UpdatedAt);
        Assert.False(note.IsUnedited);
    }

    [Fact]
    public void Edit_with_identical_values_is_a_no_op_and_leaves_UpdatedAt_alone()
    {
        var note = NewNote();

        var changed = note.Edit(
            NoteTitle.Create("Shopping list").Value,
            NoteContent.Create("Milk and eggs").Value,
            EditedOn);

        Assert.False(changed);
        Assert.Equal(CreatedOn, note.UpdatedAt);
    }

    [Fact]
    public void Edit_treats_a_whitespace_only_change_to_the_title_as_no_change()
    {
        var note = NewNote();

        // NoteTitle trims, so "  Shopping list  " is the same title.
        var changed = note.Edit(
            NoteTitle.Create("  Shopping list  ").Value,
            NoteContent.Create("Milk and eggs").Value,
            EditedOn);

        Assert.False(changed);
    }

    [Fact]
    public void Clearing_the_content_counts_as_a_change()
    {
        var note = NewNote();

        var changed = note.Edit(note.Title, NoteContent.Create(null).Value, EditedOn);

        Assert.True(changed);
        Assert.False(note.Content.HasValue);
        Assert.Equal(EditedOn, note.UpdatedAt);
    }

    [Fact]
    public void IsOwnedBy_only_recognises_the_owner()
    {
        var note = NewNote();

        Assert.True(note.IsOwnedBy(Owner));
        Assert.False(note.IsOwnedBy(Guid.NewGuid()));
    }
}
