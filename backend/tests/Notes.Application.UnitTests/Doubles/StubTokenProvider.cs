using Notes.Application.Abstractions;
using Notes.Domain.Users;

namespace Notes.Application.UnitTests.Doubles;

public sealed class StubTokenProvider : ITokenProvider
{
    public static readonly DateTime ExpiresAt = new(2026, 9, 18, 22, 0, 0, DateTimeKind.Utc);

    public AccessToken Create(User user) => new($"token-for-{user.Id}", ExpiresAt);
}
