using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Rules;
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
    /// <summary>Bitmis sayilan — yani artik iptal edilemeyen — durumlar.</summary>
    private static readonly CrawlStatus[] TerminalStatuses =
        [CrawlStatus.Completed, CrawlStatus.Partial, CrawlStatus.Failed, CrawlStatus.Cancelled];

    public async Task<StartCrawlResponse> StartAsync(
        StartCrawlRequest request, Guid tenantId, CancellationToken ct = default)
    {
        var site = await repository.GetSiteForTenantAsync(request.SiteId, tenantId, ct)
            ?? throw new NotFoundException($"Site {request.SiteId} bulunamadi");

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

        // Kuyrukta beklerken iptal edilmis olabilir.
        if (crawl.Status == CrawlStatus.Cancelled) return;

        crawl.Status = CrawlStatus.Running;
        crawl.StartedAt = DateTimeOffset.UtcNow;
        crawl.ErrorMessage = null;
        await repository.SaveChangesAsync(ct);

        try
        {
            crawl.Status = await engine.RunAsync(crawl, site, IsCancelRequestedAsync, ct);
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

        if (crawl.Status == CrawlStatus.Cancelled)
            crawl.ErrorMessage = "Tarama iptal edildi";

        crawl.FinishedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);

        async Task<bool> IsCancelRequestedAsync(CancellationToken token) =>
            await repository.ReadCrawlStatusAsync(crawlId, token) == CrawlStatus.Cancelled;
    }

    /// <summary>
    /// Iptal isaretini veritabanina yazar; calisan worker bunu bir sonraki derinlik
    /// gecisinde gorup taramayi <see cref="CrawlStatus.Cancelled"/> ile bitirir.
    /// </summary>
    public async Task<CrawlSummaryDto> CancelAsync(Guid crawlId, Guid tenantId, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlForTenantAsync(crawlId, tenantId, ct)
            ?? throw new NotFoundException($"Crawl {crawlId} bulunamadi");

        if (TerminalStatuses.Contains(crawl.Status))
            throw new InvalidOperationException($"Tarama '{crawl.Status}' durumunda — iptal edilemez");

        crawl.Status = CrawlStatus.Cancelled;
        crawl.ErrorMessage = "Tarama iptal edildi";
        crawl.FinishedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);

        return CrawlSummaryDto.From(crawl);
    }

    public async Task<CrawlSummaryDto?> GetSummaryAsync(
        Guid crawlId, Guid tenantId, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlForTenantAsync(crawlId, tenantId, ct);
        return crawl is null ? null : CrawlSummaryDto.From(crawl);
    }

    public async Task<PagedResult<CrawlListItemDto>> ListForSiteAsync(
        Guid siteId, Guid tenantId, int page, int size, CancellationToken ct = default)
    {
        _ = await repository.GetSiteForTenantAsync(siteId, tenantId, ct)
            ?? throw new NotFoundException($"Site {siteId} bulunamadi");

        var (pageNumber, pageSize) = Paging.Normalize(page, size);
        var total = await repository.CountCrawlsAsync(siteId, ct);
        var items = await repository.ListCrawlsAsync(siteId, (pageNumber - 1) * pageSize, pageSize, ct);

        return new PagedResult<CrawlListItemDto>(
            [.. items.Select(CrawlListItemDto.From)], total, pageNumber, pageSize);
    }

    public async Task<PagedResult<PageDto>?> GetPagesAsync(
        Guid crawlId, Guid tenantId, PageQuery query, int page, int size, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlForTenantAsync(crawlId, tenantId, ct);
        if (crawl is null) return null;

        var (pageNumber, pageSize) = Paging.Normalize(page, size);
        var (items, total) = await repository.QueryPagesAsync(
            crawlId, query, (pageNumber - 1) * pageSize, pageSize, ct);

        return new PagedResult<PageDto>([.. items.Select(PageDto.From)], total, pageNumber, pageSize);
    }

    public async Task<PagedResult<IssueDto>?> GetIssuesAsync(
        Guid crawlId, Guid tenantId, IssueQuery query, int page, int size, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlForTenantAsync(crawlId, tenantId, ct);
        if (crawl is null) return null;

        var (pageNumber, pageSize) = Paging.Normalize(page, size);
        var (items, total) = await repository.QueryIssuesAsync(
            crawlId, query, (pageNumber - 1) * pageSize, pageSize, ct);

        return new PagedResult<IssueDto>([.. items.Select(IssueDto.From)], total, pageNumber, pageSize);
    }

    public async Task<PageDetailDto?> GetPageAsync(Guid pageId, Guid tenantId, CancellationToken ct = default)
    {
        var page = await repository.GetPageForTenantAsync(pageId, tenantId, ct);
        if (page is null) return null;

        var issues = await repository.ListIssuesForPageAsync(pageId, ct);
        return PageDetailDto.From(page, issues);
    }

    /// <summary>
    /// Iki taramayi kiyaslar. Bulgu kimligi (kural kodu + sayfa URL'i) ciftidir — sayfa
    /// satirlari her taramada yeniden uretildigi icin id uzerinden eslesme yapilamaz.
    /// </summary>
    public async Task<CrawlCompareDto?> CompareAsync(
        Guid crawlId, Guid previousCrawlId, Guid tenantId, CancellationToken ct = default)
    {
        var current = await repository.GetCrawlForTenantAsync(crawlId, tenantId, ct);
        var previous = await repository.GetCrawlForTenantAsync(previousCrawlId, tenantId, ct);
        if (current is null || previous is null) return null;

        if (current.SiteId != previous.SiteId)
            throw new InvalidOperationException("Yalniz ayni sitenin taramalari kiyaslanabilir");

        var currentIssues = await repository.ListIssuesAsync(crawlId, ct);
        var previousIssues = await repository.ListIssuesAsync(previousCrawlId, ct);

        var previousKeys = previousIssues.Select(Key).ToHashSet(StringComparer.Ordinal);
        var currentKeys = currentIssues.Select(Key).ToHashSet(StringComparer.Ordinal);

        var newIssues = currentIssues.Where(i => !previousKeys.Contains(Key(i))).ToList();
        var resolved = previousIssues.Where(i => !currentKeys.Contains(Key(i))).ToList();

        return new CrawlCompareDto(
            current.Id,
            previous.Id,
            current.OverallScore,
            previous.OverallScore,
            current.OverallScore is decimal now && previous.OverallScore is decimal before
                ? now - before
                : null,
            current.PagesCrawled,
            previous.PagesCrawled,
            Delta(current.IssueCounts, previous.IssueCounts, (a, b) => a - b),
            Delta(current.CategoryScores, previous.CategoryScores, (a, b) => a - b),
            [.. newIssues.OrderByDescending(i => i.Severity).Select(IssueDto.From)],
            [.. resolved.OrderByDescending(i => i.Severity).Select(IssueDto.From)],
            currentIssues.Count - newIssues.Count);

        static string Key(Issue i) => $"{i.RuleCode}|{i.Page?.Url ?? string.Empty}";
    }

    private static Dictionary<string, T> Delta<T>(
        IReadOnlyDictionary<string, T> current, IReadOnlyDictionary<string, T> previous, Func<T, T, T> subtract)
        where T : struct
    {
        var result = new Dictionary<string, T>();
        foreach (var key in current.Keys.Concat(previous.Keys).Distinct())
        {
            result[key] = subtract(
                current.TryGetValue(key, out var now) ? now : default,
                previous.TryGetValue(key, out var before) ? before : default);
        }
        return result;
    }

    private async Task FinishAsync(Crawl crawl, CrawlStatus status, string? error, CancellationToken ct)
    {
        crawl.Status = status;
        crawl.ErrorMessage = error;
        crawl.FinishedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);
    }
}
