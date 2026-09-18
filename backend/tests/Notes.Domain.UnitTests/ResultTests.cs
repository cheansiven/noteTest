using Notes.Domain.Common;

namespace Notes.Domain.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void A_successful_result_exposes_its_value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Reading_the_value_of_a_failed_result_is_a_programming_error()
    {
        var result = Result.Failure<int>(Error.NotFound("X.NotFound", "Missing."));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void A_result_cannot_be_both_successful_and_carry_an_error()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }
}
