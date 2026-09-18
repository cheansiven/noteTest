using Notes.Domain.Users;

namespace Notes.Domain.UnitTests.Users;

public sealed class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last+tag@sub.example.co.uk")]
    public void Create_accepts_a_valid_address(string input)
    {
        var result = Email.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(input.ToLowerInvariant(), result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_missing_address(string? input)
    {
        var result = Email.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailRequired, result.Error);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("spaces in@example.com")]
    [InlineData("Display Name <user@example.com>")]
    public void Create_rejects_a_malformed_address(string input)
    {
        var result = Email.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailInvalid, result.Error);
    }

    [Fact]
    public void Create_accepts_a_domain_without_a_dot()
    {
        // "user@localhost" is a legitimate address, so requiring a TLD would wrongly
        // reject intranet accounts. Deliberate: only delivery truly validates an email.
        var result = Email.Create("user@localhost");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_rejects_an_address_beyond_the_column_length()
    {
        var input = new string('a', Email.MaxLength) + "@example.com";

        var result = Email.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal("User.Email.TooLong", result.Error.Code);
    }

    [Fact]
    public void Create_normalises_case_and_surrounding_whitespace()
    {
        var result = Email.Create("  User@Example.COM  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.Value);
    }

    [Fact]
    public void Two_addresses_with_the_same_value_are_equal()
    {
        var left = Email.Create("user@example.com").Value;
        var right = Email.Create("USER@example.com").Value;

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }
}
