using System.Security.Claims;
using SeoCopilot.Api.Infrastructure;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Api.Endpoints;

public static class SiteEndpoints
{
    public static IEndpointRouteBuilder MapSiteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sites").WithTags("Sites").RequireAuthorization();

        group.MapPost("/", async (
            CreateSiteRequest req, ClaimsPrincipal user, SiteService sites, CancellationToken ct) =>
        {
            var site = await sites.CreateAsync(req, user.TenantId(), ct);
            return Results.Created($"/api/sites/{site.Id}", site);
        });

        group.MapGet("/", async (ClaimsPrincipal user, SiteService sites, CancellationToken ct) =>
            Results.Ok(await sites.ListAsync(user.TenantId(), ct)));

        group.MapGet("/{siteId:guid}", async (
            Guid siteId, ClaimsPrincipal user, SiteService sites, CancellationToken ct) =>
            Results.Ok(await sites.GetAsync(siteId, user.TenantId(), ct)));

        group.MapPost("/{siteId:guid}/verify", async (
            Guid siteId, ClaimsPrincipal user, SiteService sites, CancellationToken ct) =>
            Results.Ok(await sites.VerifyAsync(siteId, user.TenantId(), ct)));

        group.MapPatch("/{siteId:guid}/crawl-settings", async (
            Guid siteId, CrawlSettingsDto req, ClaimsPrincipal user, SiteService sites, CancellationToken ct) =>
            Results.Ok(await sites.UpdateCrawlSettingsAsync(siteId, user.TenantId(), req, ct)));

        return app;
    }
}
