using NSubstitute;
using Notes.Application.Abstractions;
using Notes.Application.Contracts;
using Notes.Application.UnitTests.Doubles;
using Notes.Application.Users;
using Notes.Domain.Common;
using Notes.Domain.Users;

namespace Notes.Application.UnitTests.Users;

public sealed class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly AuthService _sut;

    public AuthServiceTests() =>
        _sut = new AuthService(_users, new FakePasswordHasher(), new StubTokenProvider(), _clock);

    private static RegisterRequest ValidRegistration => new("User@Example.com", "Test User", "Passw0rd!23");

    [Fact]
    public async Task Register_persists_the_user_and_returns_a_token()
    {
        _users.ExistsWithEmailAsync(Arg.Any<Email>()).Returns(false);

        var result = await _sut.RegisterAsync(ValidRegistration);

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.User.Email);
        Assert.Equal(StubTokenProvider.ExpiresAt, result.Value.ExpiresAt);
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email.Value == "user@example.com" && u.CreatedAt == _clock.UtcNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_delegates_hashing_rather_than_storing_the_raw_password()
    {
        _users.ExistsWithEmailAsync(Arg.Any<Email>()).Returns(false);
        var expected = new FakePasswordHasher().Hash(Password.Create("Passw0rd!23").Value);

        await _sut.RegisterAsync(ValidRegistration);

        // That the hash itself is irreversible is a property of the hasher and is
        // asserted against the real BCrypt implementation in Notes.Infrastructure.UnitTests.
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.PasswordHash.Value == expected.Value),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_rejects_an_email_that_is_already_taken()
    {
        _users.ExistsWithEmailAsync(Arg.Any<Email>()).Returns(true);

        var result = await _sut.RegisterAsync(ValidRegistration);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("not-an-email", "Test User", "Passw0rd!23")]
    [InlineData("user@example.com", "A", "Passw0rd!23")]
    [InlineData("user@example.com", "Test User", "short")]
    public async Task Register_reports_invalid_input_as_a_validation_failure(
        string email, string displayName, string password)
    {
        _users.ExistsWithEmailAsync(Arg.Any<Email>()).Returns(false);

        var result = await _sut.RegisterAsync(new RegisterRequest(email, displayName, password));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_succeeds_with_the_right_password()
    {
        _users.GetByEmailAsync(Arg.Any<Email>()).Returns(ExistingUser());

        var result = await _sut.LoginAsync(new LoginRequest("user@example.com", "Passw0rd!23"));

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.User.Email);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_fails_as_unauthorized()
    {
        _users.GetByEmailAsync(Arg.Any<Email>()).Returns(ExistingUser());

        var result = await _sut.LoginAsync(new LoginRequest("user@example.com", "WrongPassword1"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Login_cannot_be_used_to_discover_which_emails_are_registered()
    {
        // Unknown account.
        _users.GetByEmailAsync(Arg.Any<Email>()).Returns((User?)null);
        var unknown = await _sut.LoginAsync(new LoginRequest("nobody@example.com", "Passw0rd!23"));

        // Known account, wrong password.
        _users.GetByEmailAsync(Arg.Any<Email>()).Returns(ExistingUser());
        var wrongPassword = await _sut.LoginAsync(new LoginRequest("user@example.com", "WrongPassword1"));

        // Malformed input.
        var malformed = await _sut.LoginAsync(new LoginRequest("nonsense", "x"));

        Assert.Equal(unknown.Error, wrongPassword.Error);
        Assert.Equal(unknown.Error, malformed.Error);
    }

    [Fact]
    public async Task GetProfile_reports_a_missing_user_as_not_found()
    {
        _users.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        var result = await _sut.GetProfileAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    private User ExistingUser() => User.Register(
        Email.Create("user@example.com").Value,
        DisplayName.Create("Test User").Value,
        new FakePasswordHasher().Hash(Password.Create("Passw0rd!23").Value),
        _clock.UtcNow);
}
