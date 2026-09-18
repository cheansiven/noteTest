using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Notes.Application.Abstractions;
using Notes.Domain.Users;
using Notes.Infrastructure.Security;

namespace Notes.Infrastructure.UnitTests.Security;

public sealed class JwtTokenProviderTests
{
    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);
    }

    private static readonly JwtOptions Options = new()
    {
        Issuer = "NotesApi",
        Audience = "NotesApp",
        Secret = "test-signing-key-that-is-long-enough-32b+",
        ExpiryMinutes = 720,
    };

    private readonly FixedClock _clock = new();
    private readonly JwtTokenProvider _sut;

    public JwtTokenProviderTests() => _sut = new JwtTokenProvider(Microsoft.Extensions.Options.Options.Create(Options), _clock);

    private static User NewUser() => User.Register(
        Email.Create("user@example.com").Value,
        DisplayName.Create("Test User").Value,
        PasswordHash.FromHash("$2a$12$hash"),
        DateTime.UtcNow);

    [Fact]
    public void Token_carries_the_user_id_as_the_subject()
    {
        var user = NewUser();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(_sut.Create(user).Value);

        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal(user.Id.ToString(), token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
    }

    [Fact]
    public void Token_expires_after_the_configured_lifetime()
    {
        var accessToken = _sut.Create(NewUser());

        Assert.Equal(_clock.UtcNow.AddMinutes(Options.ExpiryMinutes), accessToken.ExpiresAtUtc);
    }

    [Fact]
    public void Token_is_scoped_to_the_configured_issuer_and_audience()
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(_sut.Create(NewUser()).Value);

        Assert.Equal(Options.Issuer, token.Issuer);
        Assert.Contains(Options.Audience, token.Audiences);
    }

    [Fact]
    public void Token_never_carries_the_password_hash()
    {
        var raw = _sut.Create(NewUser()).Value;
        var token = new JwtSecurityTokenHandler().ReadJwtToken(raw);

        Assert.DoesNotContain(token.Claims, c => c.Value.Contains("$2a$12$"));
    }

    [Fact]
    public void Each_token_has_a_unique_identifier()
    {
        var user = NewUser();
        var handler = new JwtSecurityTokenHandler();

        var first = handler.ReadJwtToken(_sut.Create(user).Value).Id;
        var second = handler.ReadJwtToken(_sut.Create(user).Value).Id;

        Assert.NotEqual(first, second);
    }
}
