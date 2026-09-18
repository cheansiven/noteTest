using Notes.Domain.Users;
using Notes.Infrastructure.Security;

namespace Notes.Infrastructure.UnitTests.Security;

public sealed class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    private static Password Plain(string value) => Password.Create(value).Value;

    [Fact]
    public void Hash_does_not_contain_the_plaintext_password()
    {
        var password = Plain("Passw0rd!23");

        var hash = _sut.Hash(password);

        Assert.DoesNotContain("Passw0rd!23", hash.Value);
    }

    [Fact]
    public void Hashing_the_same_password_twice_produces_different_hashes()
    {
        var password = Plain("Passw0rd!23");

        // Per-password salting: identical passwords must not produce identical rows.
        Assert.NotEqual(_sut.Hash(password).Value, _sut.Hash(password).Value);
    }

    [Fact]
    public void Verify_accepts_the_original_password()
    {
        var password = Plain("Passw0rd!23");

        Assert.True(_sut.Verify(password, _sut.Hash(password)));
    }

    [Fact]
    public void Verify_rejects_a_different_password()
    {
        var hash = _sut.Hash(Plain("Passw0rd!23"));

        Assert.False(_sut.Verify(Plain("Passw0rd!24"), hash));
    }

    [Fact]
    public void Verify_treats_a_malformed_stored_hash_as_a_failed_login_not_a_crash()
    {
        var hash = PasswordHash.FromHash("not-a-bcrypt-hash");

        Assert.False(_sut.Verify(Plain("Passw0rd!23"), hash));
    }

    [Fact]
    public void Hash_uses_a_work_factor_of_at_least_twelve()
    {
        // BCrypt encodes its cost in the prefix, e.g. "$2a$12$...".
        var hash = _sut.Hash(Plain("Passw0rd!23"));

        Assert.StartsWith("$2", hash.Value);
        var cost = int.Parse(hash.Value.Split('$')[2]);
        Assert.True(cost >= 12, $"Work factor {cost} is below the intended minimum of 12.");
    }
}
