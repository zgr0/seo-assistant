using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using SeoCopilot.Application.Common;

namespace SeoCopilot.Api.Infrastructure;

/// <summary>
/// JWT claim'lerini okur. MapInboundClaims kapali oldugundan claim adlari token'daki
/// haliyle gelir (bkz. JwtTokenService).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public const string TenantClaim = "tenant_id";

    public static Guid TenantId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(TenantClaim), out var tenantId)
            ? tenantId
            : throw new AuthException("Token'da geçerli tenant_id yok");

    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : throw new AuthException("Token'da geçerli sub yok");
}
