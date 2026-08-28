using SeoCopilot.Application.Dtos;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Api.Endpoints;

public static class CrawlEndpoints
{
    public static IEndpointRouteBuilder MapCrawlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crawls").WithTags("Crawls").RequireAuthorization();

        group.MapPost("/", async (StartCrawlRequest req, CrawlOrchestrator orchestrator, CancellationToken ct) =>
        {
            var result = await orchestrator.StartAsync(req, ct);
            return Results.Created($"/api/crawls/{result.CrawlId}", result);
        });

        group.MapGet("/{crawlId:guid}", async (Guid crawlId, CrawlOrchestrator orchestrator, CancellationToken ct) =>
        {
            var summary = await orchestrator.GetSummaryAsync(crawlId, ct);
            return summary is null ? Results.NotFound() : Results.Ok(summary);
        });

        return app;
    }
}
