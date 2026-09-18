using Notes.Application.Abstractions;
using Notes.Application.Contracts;
using Notes.Domain.Common;
using Notes.Domain.Users;

namespace Notes.Application.Users;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;
    private readonly IDateTimeProvider _clock;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider,
        IDateTimeProvider clock)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
        _clock = clock;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Every input becomes a validated value object before anything else happens.
        var email = Email.Create(request.Email);
        if (email.IsFailure) return Result.Failure<AuthResponse>(email.Error);

        var displayName = DisplayName.Create(request.DisplayName);
        if (displayName.IsFailure) return Result.Failure<AuthResponse>(displayName.Error);

        var password = Password.Create(request.Password);
        if (password.IsFailure) return Result.Failure<AuthResponse>(password.Error);

        if (await _users.ExistsWithEmailAsync(email.Value, cancellationToken))
        {
            return Result.Failure<AuthResponse>(UserErrors.EmailAlreadyRegistered);
        }

        var user = User.Register(
            email.Value,
            displayName.Value,
            _passwordHasher.Hash(password.Value),
            _clock.UtcNow);

        await _users.AddAsync(user, cancellationToken);

        return Result.Success(BuildResponse(user));
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = Email.Create(request.Email);
        var password = Password.Create(request.Password);

        // A malformed email or too-short password is reported as bad credentials rather
        // than a validation error, so the endpoint gives away nothing about the rules
        // or about which accounts exist.
        if (email.IsFailure || password.IsFailure)
        {
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);
        }

        var user = await _users.GetByEmailAsync(email.Value, cancellationToken);

        if (user is null || !_passwordHasher.Verify(password.Value, user.PasswordHash))
        {
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);
        }

        return Result.Success(BuildResponse(user));
    }

    public async Task<Result<UserDto>> GetProfileAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);

        return user is null
            ? Result.Failure<UserDto>(UserErrors.NotFound)
            : Result.Success(ToDto(user));
    }

    private AuthResponse BuildResponse(User user)
    {
        var token = _tokenProvider.Create(user);
        return new AuthResponse(token.Value, token.ExpiresAtUtc, ToDto(user));
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email.Value, user.DisplayName.Value, user.CreatedAt);
}
