using Notes.Application.Contracts;
using Notes.Domain.Common;

namespace Notes.Application.Users;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
