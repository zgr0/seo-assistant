namespace SeoCopilot.Application.Dtos;

public record RegisterRequest(string Email, string Password, string FullName, string TenantName);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record AuthResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserInfo User);

public record UserInfo(Guid Id, string Email, string FullName, string Role, Guid TenantId);
