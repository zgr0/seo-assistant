using Hangfire;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Api.Adapters;

/// <summary>ICrawlQueue -> Hangfire fire-and-forget job.</summary>
public sealed class HangfireCrawlQueue(IBackgroundJobClient jobs) : ICrawlQueue
{
    public void Enqueue(Guid crawlId) =>
        jobs.Enqueue<CrawlJob>(j => j.RunAsync(crawlId, CancellationToken.None));
}

/// <summary>Hangfire worker giris noktasi — DI'dan orchestrator alir.</summary>
public sealed class CrawlJob(CrawlOrchestrator orchestrator)
{
    public Task RunAsync(Guid crawlId, CancellationToken ct) => orchestrator.RunAsync(crawlId, ct);
}
