using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services;

/// <summary>
/// Ana use-case: dogrulanmis bir site icin crawl kuyruga at; worker cagrisinda
/// <see cref="CrawlEngine"/>'i calistir, durumu ve hatayi yaz.
/// </summary>
public sealed class CrawlOrchestrator(
    ISiteRepository repository,
    CrawlEngine engine,
    ICrawlQueue queue)
{
    public async Task<StartCrawlResponse> StartAsync(
        StartCrawlRequest request, Guid tenantId, CancellationToken ct = default)
    {
        var site = await repository.GetSiteForTenantAsync(request.SiteId, tenantId, ct)
            ?? throw new NotFoundException($"Site {request.SiteId} bulunamadi");

        if (site.VerifiedAt is null)
            throw new InvalidOperationException("Site dogrulanmadan tarama baslatilamaz");

        var crawl = new Crawl
        {
            SiteId = site.Id,
            Status = CrawlStatus.Queued,
            Trigger = CrawlTrigger.Manual
        };

        await repository.AddCrawlAsync(crawl, ct);
        await repository.SaveChangesAsync(ct);

        queue.Enqueue(crawl.Id);
        return new StartCrawlResponse(crawl.Id);
    }

    /// <summary>Hangfire worker tarafindan cagrilir.</summary>
    public async Task RunAsync(Guid crawlId, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlAsync(crawlId, ct)
            ?? throw new NotFoundException($"Crawl {crawlId} bulunamadi");
        var site = await repository.GetSiteAsync(crawl.SiteId, ct)
            ?? throw new NotFoundException($"Site {crawl.SiteId} bulunamadi");

        crawl.Status = CrawlStatus.Running;
        crawl.StartedAt = DateTimeOffset.UtcNow;
        crawl.ErrorMessage = null;
        await repository.SaveChangesAsync(ct);

        try
        {
            crawl.Status = await engine.RunAsync(crawl, site, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await FinishAsync(crawl, CrawlStatus.Cancelled, "Tarama iptal edildi", CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await FinishAsync(crawl, CrawlStatus.Failed, ex.Message, ct);
            throw;
        }

        crawl.FinishedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);
    }

    public async Task<CrawlSummaryDto?> GetSummaryAsync(
        Guid crawlId, Guid tenantId, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlForTenantAsync(crawlId, tenantId, ct);
        return crawl is null ? null : CrawlSummaryDto.From(crawl);
    }

    public async Task<PagedResult<PageDto>?> GetPagesAsync(
        Guid crawlId, Guid tenantId, int page, int size, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlForTenantAsync(crawlId, tenantId, ct);
        if (crawl is null) return null;

        var pageNumber = Math.Max(1, page);
        var pageSize = Math.Clamp(size, 1, 200);

        var total = await repository.CountPagesAsync(crawlId, ct);
        var items = await repository.GetPagesAsync(crawlId, (pageNumber - 1) * pageSize, pageSize, ct);

        return new PagedResult<PageDto>([.. items.Select(PageDto.From)], total, pageNumber, pageSize);
    }

    public async Task<IReadOnlyList<IssueDto>?> GetIssuesAsync(
        Guid crawlId, Guid tenantId, Severity? minSeverity, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlForTenantAsync(crawlId, tenantId, ct);
        if (crawl is null) return null;

        return [.. crawl.Issues
            .Where(i => minSeverity is null || i.Severity >= minSeverity)
            .OrderByDescending(i => i.Severity)
            .Select(IssueDto.From)];
    }

    private async Task FinishAsync(Crawl crawl, CrawlStatus status, string? error, CancellationToken ct)
    {
        crawl.Status = status;
        crawl.ErrorMessage = error;
        crawl.FinishedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);
    }
}
