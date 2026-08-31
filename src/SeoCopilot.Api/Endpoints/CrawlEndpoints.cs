using System.Security.Claims;
using SeoCopilot.Api.Infrastructure;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Application.Services;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Api.Endpoints;

public static class CrawlEndpoints
{
    public static IEndpointRouteBuilder MapCrawlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crawls").WithTags("Crawls").RequireAuthorization();

        group.MapPost("/", async (
            StartCrawlRequest req, ClaimsPrincipal user, CrawlOrchestrator orchestrator, CancellationToken ct) =>
        {
            var result = await orchestrator.StartAsync(req, user.TenantId(), ct);
            return Results.Created($"/api/crawls/{result.CrawlId}", result);
        });

        group.MapGet("/{crawlId:guid}", async (
            Guid crawlId, ClaimsPrincipal user, CrawlOrchestrator orchestrator, CancellationToken ct) =>
        {
            var summary = await orchestrator.GetSummaryAsync(crawlId, user.TenantId(), ct);
            return summary is null ? Results.NotFound() : Results.Ok(summary);
        });

        group.MapGet("/{crawlId:guid}/pages", async (
            Guid crawlId, ClaimsPrincipal user, CrawlOrchestrator orchestrator,
            int? page, int? size, CancellationToken ct) =>
        {
            var result = await orchestrator.GetPagesAsync(crawlId, user.TenantId(), page ?? 1, size ?? 50, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/{crawlId:guid}/issues", async (
            Guid crawlId, ClaimsPrincipal user, CrawlOrchestrator orchestrator,
            string? minSeverity, CancellationToken ct) =>
        {
            Severity? severity = Enum.TryParse<Severity>(minSeverity, ignoreCase: true, out var parsed)
                ? parsed
                : null;

            var issues = await orchestrator.GetIssuesAsync(crawlId, user.TenantId(), severity, ct);
            return issues is null ? Results.NotFound() : Results.Ok(issues);
        });

        return app;
    }
}
