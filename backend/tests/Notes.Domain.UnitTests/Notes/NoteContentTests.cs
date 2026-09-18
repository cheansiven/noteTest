using Notes.Domain.Notes;

namespace Notes.Domain.UnitTests.Notes;

public sealed class NoteContentTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Blank_input_collapses_to_empty_so_the_column_is_never_a_blank_string(string? input)
    {
        var result = NoteContent.Create(input);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasValue);
        Assert.Null(result.Value.Value);
    }

    [Fact]
    public void Content_is_trimmed_but_inner_line_breaks_are_preserved()
    {
        var result = NoteContent.Create("  first line\nsecond line  ");

        Assert.Equal("first line\nsecond line", result.Value.Value);
    }

    [Fact]
    public void Create_rejects_content_past_the_limit()
    {
        var result = NoteContent.Create(new string('a', NoteContent.MaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal("Note.Content.TooLong", result.Error.Code);
    }

    [Fact]
    public void Empty_instances_are_equal_to_each_other()
    {
        Assert.Equal(NoteContent.Empty, NoteContent.Create("   ").Value);
    }
}
