using System.Security.Claims;
using Sitecraft.Api.Infrastructure;
using Sitecraft.Application.Dtos;
using Sitecraft.Application.Services.Social;

namespace Sitecraft.Api.Endpoints;

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

        // Formdaki "Yapay zeka ile uret" dugmesi: acik mi, gunluk sinir, bugun kullanilan.
        group.MapGet("/image-settings", async (
            ClaimsPrincipal user, SocialKitService social, CancellationToken ct) =>
            Results.Ok(await social.GetImageSettingsAsync(user.TenantId(), ct)));

        return app;
    }
}
