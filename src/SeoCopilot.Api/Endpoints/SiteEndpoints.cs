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

        group.MapPatch("/{siteId:guid}", async (
            Guid siteId, UpdateSiteRequest req, ClaimsPrincipal user, SiteService sites, CancellationToken ct) =>
            Results.Ok(await sites.UpdateAsync(siteId, user.TenantId(), req, ct)));

        group.MapDelete("/{siteId:guid}", async (
            Guid siteId, ClaimsPrincipal user, SiteService sites, CancellationToken ct) =>
        {
            await sites.DeleteAsync(siteId, user.TenantId(), ct);
            return Results.NoContent();
        });

        group.MapPost("/{siteId:guid}/verify", async (
            Guid siteId, ClaimsPrincipal user, SiteService sites, CancellationToken ct) =>
            Results.Ok(await sites.VerifyAsync(siteId, user.TenantId(), ct)));

        group.MapPatch("/{siteId:guid}/crawl-settings", async (
            Guid siteId, CrawlSettingsDto req, ClaimsPrincipal user, SiteService sites, CancellationToken ct) =>
            Results.Ok(await sites.UpdateCrawlSettingsAsync(siteId, user.TenantId(), req, ct)));

        // --- taramalar ---

        group.MapPost("/{siteId:guid}/crawls", async (
            Guid siteId, ClaimsPrincipal user, CrawlOrchestrator orchestrator, CancellationToken ct) =>
        {
            var result = await orchestrator.StartAsync(new StartCrawlRequest(siteId), user.TenantId(), ct);
            return Results.Created($"/api/crawls/{result.CrawlId}", result);
        });

        group.MapGet("/{siteId:guid}/crawls", async (
            Guid siteId, ClaimsPrincipal user, CrawlOrchestrator orchestrator,
            int? page, int? size, CancellationToken ct) =>
            Results.Ok(await orchestrator.ListForSiteAsync(siteId, user.TenantId(), page ?? 1, size ?? 20, ct)));

        // --- performans ---

        group.MapGet("/{siteId:guid}/vitals", async (
            Guid siteId, ClaimsPrincipal user, VitalsService vitals,
            string? url, int? take, CancellationToken ct) =>
            Results.Ok(await vitals.GetAsync(siteId, user.TenantId(), url, take, ct)));

        // --- raporlar ---

        group.MapPost("/{siteId:guid}/reports", async (
            Guid siteId, CreateReportRequest? req, ClaimsPrincipal user,
            ReportService reports, CancellationToken ct) =>
        {
            var report = await reports.CreateAsync(siteId, user.TenantId(), req ?? new CreateReportRequest(), ct);
            return Results.Created($"/api/reports/{report.Id}", report);
        });

        group.MapGet("/{siteId:guid}/reports", async (
            Guid siteId, ClaimsPrincipal user, ReportService reports, CancellationToken ct) =>
            Results.Ok(await reports.ListAsync(siteId, user.TenantId(), ct)));

        return app;
    }
}
