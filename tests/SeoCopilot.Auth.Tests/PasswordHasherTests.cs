using SeoCopilot.Infrastructure.Auth;

namespace SeoCopilot.Auth.Tests;

public class PasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_then_verify_roundtrips()
    {
        var hash = _hasher.Hash("sifre12345");

        Assert.NotEqual("sifre12345", hash);
        Assert.StartsWith("$2", hash);
        Assert.True(_hasher.Verify("sifre12345", hash));
    }

    [Fact]
    public void Verify_rejects_wrong_password()
    {
        var hash = _hasher.Hash("dogru-sifre");
        Assert.False(_hasher.Verify("yanlis-sifre", hash));
    }

    [Fact]
    public void Verify_returns_false_for_malformed_hash()
    {
        Assert.False(_hasher.Verify("x", "not-a-bcrypt-hash"));
    }

    [Fact]
    public void Same_password_produces_different_hashes()
    {
        Assert.NotEqual(_hasher.Hash("aaaaaaaa"), _hasher.Hash("aaaaaaaa"));
    }
}
