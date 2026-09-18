namespace Notes.Application.Contracts;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserDto(Guid Id, string Email, string DisplayName, DateTime CreatedAt);

public sealed record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);
