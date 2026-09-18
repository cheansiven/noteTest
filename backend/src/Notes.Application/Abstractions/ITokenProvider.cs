using Notes.Domain.Users;

namespace Notes.Application.Abstractions;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface ITokenProvider
{
    AccessToken Create(User user);
}
