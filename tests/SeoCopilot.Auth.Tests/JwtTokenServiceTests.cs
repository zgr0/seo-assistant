using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using SeoCopilot.Domain.Entities.Tenancy;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Auth;

namespace SeoCopilot.Auth.Tests;

public class JwtTokenServiceTests
{
    private static readonly JwtOptions Opt = new()
    {
        Key = "unit-test-signing-key-that-is-long-enough-32b+",
        Issuer = "seocopilot",
        Audience = "seocopilot",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 30
    };

    private readonly JwtTokenService _svc = new(Options.Create(Opt));

    private static User SampleUser() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Email = "u@example.com",
        FullName = "Ornek",
        Role = UserRole.Owner
    };

    [Fact]
    public void Access_token_carries_expected_claims()
    {
        var user = SampleUser();

        var token = _svc.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        Assert.Equal("seocopilot", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "seocopilot");
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(user.TenantId.ToString(), jwt.Claims.First(c => c.Type == "tenant_id").Value);
        Assert.Equal("owner", jwt.Claims.First(c => c.Type == "role").Value);
        Assert.True(token.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(10));
    }

    [Fact]
    public void Refresh_token_raw_hashes_consistently()
    {
        var pair = _svc.CreateRefreshToken();

        Assert.Equal(pair.TokenHash, _svc.HashRefreshToken(pair.RawToken));
        Assert.Equal(32, pair.TokenHash.Length); // sha256
        Assert.True(pair.ExpiresAt > DateTimeOffset.UtcNow.AddDays(29));
    }

    [Fact]
    public void Refresh_tokens_are_unique()
    {
        Assert.NotEqual(_svc.CreateRefreshToken().RawToken, _svc.CreateRefreshToken().RawToken);
    }
}
