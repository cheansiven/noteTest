using Notes.Domain.Notes;

namespace Notes.Domain.UnitTests.Notes;

public sealed class NoteTitleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Create_rejects_a_blank_title(string? input)
    {
        var result = NoteTitle.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal(NoteErrors.TitleRequired, result.Error);
    }

    [Fact]
    public void Create_trims_surrounding_whitespace()
    {
        var result = NoteTitle.Create("  Meeting notes  ");

        Assert.Equal("Meeting notes", result.Value.Value);
    }

    [Fact]
    public void Create_accepts_a_title_exactly_at_the_limit()
    {
        var result = NoteTitle.Create(new string('a', NoteTitle.MaxLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_rejects_a_title_one_character_past_the_limit()
    {
        var result = NoteTitle.Create(new string('a', NoteTitle.MaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal("Note.Title.TooLong", result.Error.Code);
    }
}
