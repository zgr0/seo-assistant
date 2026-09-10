using System.Security.Claims;
using SeoCopilot.Api.Infrastructure;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Application.Services.Social;

namespace SeoCopilot.Api.Endpoints;

public static class SocialEndpoints
{
    public static IEndpointRouteBuilder MapSocialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/social").WithTags("Social").RequireAuthorization();

        // Sonuclar mevcut icerik uclarindan okunur: GET /api/content/jobs/{id}
        group.MapPost("/kits", async (
            CreateSocialKitRequest req, ClaimsPrincipal user,
            SocialKitService social, CancellationToken ct) =>
        {
            var result = await social.CreateAsync(req, user.TenantId(), user.UserId(), ct);
            return Results.Accepted("/api/content/jobs", result);
        });

        return app;
    }
}
