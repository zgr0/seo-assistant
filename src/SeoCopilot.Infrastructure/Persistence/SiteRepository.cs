using Microsoft.EntityFrameworkCore;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Performance;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Infrastructure.Persistence;

public sealed class SiteRepository(SeoCopilotDbContext db) : ISiteRepository
{
    public Task<Site?> GetSiteAsync(Guid siteId, CancellationToken ct = default) =>
        db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, ct);

    public Task<Site?> GetSiteForTenantAsync(Guid siteId, Guid tenantId, CancellationToken ct = default) =>
        db.Sites.FirstOrDefaultAsync(s => s.Id == siteId && s.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<Site>> ListSitesAsync(Guid tenantId, CancellationToken ct = default) =>
        await db.Sites
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task AddSiteAsync(Site site, CancellationToken ct = default) =>
        await db.Sites.AddAsync(site, ct);

    public Task RemoveSiteAsync(Site site, CancellationToken ct = default)
    {
        db.Sites.Remove(site);
        return Task.CompletedTask;
    }

    /// <summary>Pages dahil edilmez — bir crawl'da 500 satira kadar cikabilir.</summary>
    public Task<Crawl?> GetCrawlAsync(Guid crawlId, CancellationToken ct = default) =>
        db.Crawls
            .Include(c => c.Issues)
            .FirstOrDefaultAsync(c => c.Id == crawlId, ct);

    public Task<Crawl?> GetCrawlForTenantAsync(Guid crawlId, Guid tenantId, CancellationToken ct = default) =>
        db.Crawls
            .Include(c => c.Issues)
            .FirstOrDefaultAsync(c => c.Id == crawlId && c.Site!.TenantId == tenantId, ct);

    /// <summary>Skaler izdusum — izlenen entity'yi atlayip satirin guncel durumunu okur.</summary>
    public async Task<CrawlStatus?> ReadCrawlStatusAsync(Guid crawlId, CancellationToken ct = default) =>
        await db.Crawls
            .Where(c => c.Id == crawlId)
            .Select(c => (CrawlStatus?)c.Status)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Crawl>> ListCrawlsAsync(
        Guid siteId, int skip, int take, CancellationToken ct = default) =>
        await db.Crawls
            .Where(c => c.SiteId == siteId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountCrawlsAsync(Guid siteId, CancellationToken ct = default) =>
        db.Crawls.CountAsync(c => c.SiteId == siteId, ct);

    public Task<Crawl?> GetLatestCrawlAsync(Guid siteId, CancellationToken ct = default) =>
        db.Crawls
            .Include(c => c.Issues)
            .Where(c => c.SiteId == siteId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Crawl>> ListRecentCrawlsForTenantAsync(
        Guid tenantId, int take, CancellationToken ct = default) =>
        await db.Crawls
            .Where(c => c.Site!.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task AddCrawlAsync(Crawl crawl, CancellationToken ct = default) =>
        await db.Crawls.AddAsync(crawl, ct);

    public async Task AddPagesAsync(IEnumerable<Page> pages, CancellationToken ct = default) =>
        await db.Pages.AddRangeAsync(pages, ct);

    public async Task AddPageLinksAsync(IEnumerable<PageLink> links, CancellationToken ct = default) =>
        await db.PageLinks.AddRangeAsync(links, ct);

    public async Task AddVitalAsync(Vital vital, CancellationToken ct = default) =>
        await db.Vitals.AddAsync(vital, ct);

    public async Task<IReadOnlyList<Page>> GetPagesAsync(
        Guid crawlId, int skip, int take, CancellationToken ct = default) =>
        await db.Pages
            .Where(p => p.CrawlId == crawlId)
            .OrderBy(p => p.Depth).ThenBy(p => p.Url)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Page> Items, int Total)> QueryPagesAsync(
        Guid crawlId, PageQuery query, int skip, int take, CancellationToken ct = default)
    {
        var q = db.Pages.Where(p => p.CrawlId == crawlId);

        if (!string.IsNullOrWhiteSpace(query.UrlContains))
        {
            var needle = query.UrlContains.Trim();
            q = q.Where(p => EF.Functions.ILike(p.Url, $"%{needle}%"));
        }
        if (query.StatusCode is int status) q = q.Where(p => p.StatusCode == status);
        if (query.MinStatusCode is int min) q = q.Where(p => p.StatusCode >= min);
        if (query.MaxStatusCode is int max) q = q.Where(p => p.StatusCode <= max);
        if (query.Depth is int depth) q = q.Where(p => p.Depth == depth);
        if (query.HasIssues is bool hasIssues)
        {
            q = hasIssues
                ? q.Where(p => db.Issues.Any(i => i.PageId == p.Id))
                : q.Where(p => !db.Issues.Any(i => i.PageId == p.Id));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(p => p.Depth).ThenBy(p => p.Url)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<int> CountPagesAsync(Guid crawlId, CancellationToken ct = default) =>
        db.Pages.CountAsync(p => p.CrawlId == crawlId, ct);

    public Task<Page?> GetPageForTenantAsync(Guid pageId, Guid tenantId, CancellationToken ct = default) =>
        db.Pages
            .Include(p => p.Crawl)
            .FirstOrDefaultAsync(p => p.Id == pageId && p.Crawl!.Site!.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<Issue>> ListIssuesForPageAsync(
        Guid pageId, CancellationToken ct = default) =>
        await db.Issues
            .Include(i => i.Rule)
            .Where(i => i.PageId == pageId)
            .OrderByDescending(i => i.Severity)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Issue> Items, int Total)> QueryIssuesAsync(
        Guid crawlId, IssueQuery query, int skip, int take, CancellationToken ct = default)
    {
        var q = db.Issues
            .Include(i => i.Rule)
            .Include(i => i.Page)
            .Where(i => i.CrawlId == crawlId);

        if (query.Severity is Severity severity) q = q.Where(i => i.Severity == severity);
        if (query.MinSeverity is Severity minSeverity) q = q.Where(i => i.Severity >= minSeverity);
        if (query.Category is RuleCategory category) q = q.Where(i => i.Rule!.Category == category);
        if (!string.IsNullOrWhiteSpace(query.RuleCode))
        {
            var code = query.RuleCode.Trim().ToUpperInvariant();
            q = q.Where(i => i.RuleCode == code);
        }
        if (query.Status is IssueStatus status) q = q.Where(i => i.Status == status);
        if (query.PageId is Guid pageId) q = q.Where(i => i.PageId == pageId);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(i => i.Severity)
            .ThenBy(i => i.RuleCode)
            .ThenBy(i => i.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<Issue>> ListIssuesAsync(Guid crawlId, CancellationToken ct = default) =>
        await db.Issues
            .Include(i => i.Rule)
            .Include(i => i.Page)
            .Where(i => i.CrawlId == crawlId)
            .OrderByDescending(i => i.Severity)
            .ToListAsync(ct);

    public Task<Issue?> GetIssueForTenantAsync(long issueId, Guid tenantId, CancellationToken ct = default) =>
        db.Issues
            .Include(i => i.Rule)
            .Include(i => i.Page)
            .FirstOrDefaultAsync(i => i.Id == issueId && i.Site!.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<Issue>> ListSiteIssuesByRuleAsync(
        Guid siteId, string ruleCode, CancellationToken ct = default) =>
        await db.Issues
            .Where(i => i.SiteId == siteId && i.RuleCode == ruleCode && i.Status == IssueStatus.Open)
            .ToListAsync(ct);

    public async Task AddIssueIgnoreAsync(IssueIgnore ignore, CancellationToken ct = default) =>
        await db.IssueIgnores.AddAsync(ignore, ct);

    public Task<IssueIgnore?> FindIssueIgnoreAsync(
        Guid siteId, string ruleCode, string? urlPattern, CancellationToken ct = default) =>
        db.IssueIgnores.FirstOrDefaultAsync(
            x => x.SiteId == siteId && x.RuleCode == ruleCode && x.UrlPattern == urlPattern, ct);

    public Task RemoveIssueIgnoreAsync(IssueIgnore ignore, CancellationToken ct = default)
    {
        db.IssueIgnores.Remove(ignore);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Vital>> ListVitalsAsync(
        Guid siteId, string? url, int take, CancellationToken ct = default)
    {
        var q = db.Vitals.Where(v => v.SiteId == siteId);
        if (!string.IsNullOrWhiteSpace(url)) q = q.Where(v => v.Url == url);

        return await q
            .OrderByDescending(v => v.CollectedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
