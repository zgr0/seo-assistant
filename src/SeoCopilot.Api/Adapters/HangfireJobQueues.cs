using Hangfire;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Api.Adapters;

/// <summary>IContentQueue -> Hangfire fire-and-forget job.</summary>
public sealed class HangfireContentQueue(IBackgroundJobClient jobs) : IContentQueue
{
    public void Enqueue(Guid jobId) =>
        jobs.Enqueue<ContentJobRunner>(r => r.RunAsync(jobId, CancellationToken.None));
}

public sealed class ContentJobRunner(ContentService content)
{
    public Task RunAsync(Guid jobId, CancellationToken ct) => content.RunAsync(jobId, ct);
}

/// <summary>IReportQueue -> Hangfire fire-and-forget job.</summary>
public sealed class HangfireReportQueue(IBackgroundJobClient jobs) : IReportQueue
{
    public void Enqueue(Guid reportId) =>
        jobs.Enqueue<ReportJobRunner>(r => r.RunAsync(reportId, CancellationToken.None));
}

public sealed class ReportJobRunner(ReportService reports)
{
    public Task RunAsync(Guid reportId, CancellationToken ct) => reports.RunAsync(reportId, ct);
}
