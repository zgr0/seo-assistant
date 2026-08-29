using SeoCopilot.Domain.Entities.Tenancy;

namespace SeoCopilot.Application.Abstractions;

public interface ITokenService
{
    /// <summary>Kullanici icin imzali JWT access token uretir.</summary>
    AccessToken CreateAccessToken(User user);

    /// <summary>Rastgele refresh token — istemciye ham deger, veritabanina hash gider.</summary>
    RefreshTokenPair CreateRefreshToken();

    /// <summary>Ham refresh token degerini saklama/karsilastirma icin hash'ler.</summary>
    byte[] HashRefreshToken(string rawToken);
}

public record AccessToken(string Token, DateTimeOffset ExpiresAt);

public record RefreshTokenPair(string RawToken, byte[] TokenHash, DateTimeOffset ExpiresAt);
