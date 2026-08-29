using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Infrastructure.Auth;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    // work factor 12 — ~250ms/hash, brute-force'a karsi makul.
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
