using System.Security.Claims;
using SeoCopilot.Api.Infrastructure;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
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

        group.MapPost("/{crawlId:guid}/cancel", async (
            Guid crawlId, ClaimsPrincipal user, CrawlOrchestrator orchestrator, CancellationToken ct) =>
            Results.Ok(await orchestrator.CancelAsync(crawlId, user.TenantId(), ct)));

        group.MapGet("/{crawlId:guid}/pages", async (
            Guid crawlId, ClaimsPrincipal user, CrawlOrchestrator orchestrator,
            string? url, int? statusCode, int? minStatusCode, int? maxStatusCode,
            int? depth, bool? hasIssues, int? page, int? size, CancellationToken ct) =>
        {
            var query = new PageQuery(url, statusCode, minStatusCode, maxStatusCode, depth, hasIssues);
            var result = await orchestrator.GetPagesAsync(
                crawlId, user.TenantId(), query, page ?? 1, size ?? Paging.DefaultSize, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/{crawlId:guid}/issues", async (
            Guid crawlId, ClaimsPrincipal user, CrawlOrchestrator orchestrator,
            string? severity, string? minSeverity, string? category, string? ruleCode,
            string? status, Guid? pageId, int? page, int? size, CancellationToken ct) =>
        {
            var query = new IssueQuery(
                EnumText.ParseOptional<Severity>(severity, "severity"),
                EnumText.ParseOptional<Severity>(minSeverity, "minSeverity"),
                EnumText.ParseOptional<RuleCategory>(category, "category"),
                ruleCode,
                EnumText.ParseOptional<IssueStatus>(status, "status"),
                pageId);

            var result = await orchestrator.GetIssuesAsync(
                crawlId, user.TenantId(), query, page ?? 1, size ?? Paging.DefaultSize, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/{crawlId:guid}/compare/{previousCrawlId:guid}", async (
            Guid crawlId, Guid previousCrawlId, ClaimsPrincipal user,
            CrawlOrchestrator orchestrator, CancellationToken ct) =>
        {
            var result = await orchestrator.CompareAsync(crawlId, previousCrawlId, user.TenantId(), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return app;
    }
}

public static class PageEndpoints
{
    public static IEndpointRouteBuilder MapPageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pages").WithTags("Pages").RequireAuthorization();

        group.MapGet("/{pageId:guid}", async (
            Guid pageId, ClaimsPrincipal user, CrawlOrchestrator orchestrator, CancellationToken ct) =>
        {
            var page = await orchestrator.GetPageAsync(pageId, user.TenantId(), ct);
            return page is null ? Results.NotFound() : Results.Ok(page);
        });

        return app;
    }
}

public static class IssueEndpoints
{
    public static IEndpointRouteBuilder MapIssueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/issues").WithTags("Issues").RequireAuthorization();

        group.MapPost("/{issueId:long}/ignore", async (
            long issueId, IgnoreIssueRequest req, ClaimsPrincipal user,
            IssueService issues, CancellationToken ct) =>
            Results.Ok(await issues.IgnoreAsync(issueId, user.TenantId(), user.UserId(), req, ct)));

        group.MapPost("/{issueId:long}/reopen", async (
            long issueId, ClaimsPrincipal user, IssueService issues, CancellationToken ct) =>
            Results.Ok(await issues.ReopenAsync(issueId, user.TenantId(), ct)));

        return app;
    }
}
