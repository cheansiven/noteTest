using Notes.Domain.Users;

namespace Notes.Domain.UnitTests.Users;

public sealed class PasswordTests
{
    [Fact]
    public void Create_accepts_a_password_at_the_minimum_length()
    {
        var result = Password.Create(new string('a', Password.MinLength));

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Create_rejects_a_password_below_the_minimum_length(string input)
    {
        var result = Password.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal("User.Password.Length", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_a_password_beyond_the_maximum_length()
    {
        var result = Password.Create(new string('a', Password.MaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal("User.Password.Length", result.Error.Code);
    }

    [Fact]
    public void ToString_never_reveals_the_secret()
    {
        var password = Password.Create("correct horse battery staple").Value;

        Assert.DoesNotContain("horse", password.ToString());
    }
}
