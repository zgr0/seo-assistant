using Microsoft.EntityFrameworkCore;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Sites;

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

    /// <summary>Pages dahil edilmez — bir crawl'da 500 satira kadar cikabilir.</summary>
    public Task<Crawl?> GetCrawlAsync(Guid crawlId, CancellationToken ct = default) =>
        db.Crawls
            .Include(c => c.Issues)
            .FirstOrDefaultAsync(c => c.Id == crawlId, ct);

    public Task<Crawl?> GetCrawlForTenantAsync(Guid crawlId, Guid tenantId, CancellationToken ct = default) =>
        db.Crawls
            .Include(c => c.Issues)
            .FirstOrDefaultAsync(c => c.Id == crawlId && c.Site!.TenantId == tenantId, ct);

    public async Task AddCrawlAsync(Crawl crawl, CancellationToken ct = default) =>
        await db.Crawls.AddAsync(crawl, ct);

    public async Task AddPagesAsync(IEnumerable<Page> pages, CancellationToken ct = default) =>
        await db.Pages.AddRangeAsync(pages, ct);

    public async Task AddPageLinksAsync(IEnumerable<PageLink> links, CancellationToken ct = default) =>
        await db.PageLinks.AddRangeAsync(links, ct);

    public async Task<IReadOnlyList<Page>> GetPagesAsync(
        Guid crawlId, int skip, int take, CancellationToken ct = default) =>
        await db.Pages
            .Where(p => p.CrawlId == crawlId)
            .OrderBy(p => p.Depth).ThenBy(p => p.Url)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountPagesAsync(Guid crawlId, CancellationToken ct = default) =>
        db.Pages.CountAsync(p => p.CrawlId == crawlId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
