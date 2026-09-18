using Notes.Application.Abstractions;
using Notes.Domain.Users;

namespace Notes.Application.UnitTests.Doubles;

/// <summary>
/// Deterministic stand-in for BCrypt. These tests are about the use-case logic, not the
/// hashing algorithm, and real BCrypt would make every test ~100ms slower.
/// </summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public PasswordHash Hash(Password password) => PasswordHash.FromHash($"hashed:{password.Value}");

    public bool Verify(Password password, PasswordHash hash) => hash.Value == $"hashed:{password.Value}";
}
