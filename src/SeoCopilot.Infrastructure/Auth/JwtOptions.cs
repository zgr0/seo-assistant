namespace SeoCopilot.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "seocopilot";
    public string Audience { get; set; } = "seocopilot";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
