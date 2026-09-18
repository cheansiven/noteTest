using Notes.Domain.Users;

namespace Notes.Domain.UnitTests.Users;

public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);

    private static User NewUser() => User.Register(
        Email.Create("user@example.com").Value,
        DisplayName.Create("Test User").Value,
        PasswordHash.FromHash("$2a$12$hashedvalue"),
        Now);

    [Fact]
    public void Register_stamps_the_supplied_time_and_assigns_an_id()
    {
        var user = NewUser();

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(Now, user.CreatedAt);
        Assert.Equal("user@example.com", user.Email.Value);
    }

    [Fact]
    public void ChangePassword_replaces_the_stored_hash()
    {
        var user = NewUser();

        user.ChangePassword(PasswordHash.FromHash("$2a$12$anotherhash"));

        Assert.Equal("$2a$12$anotherhash", user.PasswordHash.Value);
    }

    [Fact]
    public void Users_are_compared_by_identity_not_by_contents()
    {
        var user = NewUser();
        var reloaded = User.FromPersistence(
            user.Id, "user@example.com", "Different Name", "$2a$12$other", Now);

        Assert.Equal(user, reloaded);
        Assert.NotEqual(user, NewUser());
    }

    [Fact]
    public void PasswordHash_rejects_an_empty_value()
    {
        Assert.Throws<ArgumentException>(() => PasswordHash.FromHash("  "));
    }
}
