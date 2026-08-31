using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Performance;
using SeoCopilot.Domain.Entities.Sites;

namespace SeoCopilot.Application.Abstractions;

public interface ISiteRepository
{
    Task<Site?> GetSiteAsync(Guid siteId, CancellationToken ct = default);

    /// <summary>Kiraci sinirini uygular — baska tenant'in sitesi icin null doner.</summary>
    Task<Site?> GetSiteForTenantAsync(Guid siteId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<Site>> ListSitesAsync(Guid tenantId, CancellationToken ct = default);

    Task AddSiteAsync(Site site, CancellationToken ct = default);

    /// <summary>Crawl'i Issues ile birlikte getirir (Pages haric — 500 satir olabilir).</summary>
    Task<Crawl?> GetCrawlAsync(Guid crawlId, CancellationToken ct = default);

    /// <summary>Kiraci sinirini uygular; crawl → site → tenant zinciri uzerinden.</summary>
    Task<Crawl?> GetCrawlForTenantAsync(Guid crawlId, Guid tenantId, CancellationToken ct = default);

    Task AddCrawlAsync(Crawl crawl, CancellationToken ct = default);

    Task AddPagesAsync(IEnumerable<Page> pages, CancellationToken ct = default);

    Task AddPageLinksAsync(IEnumerable<PageLink> links, CancellationToken ct = default);

    /// <summary>PSI olcumu — performans kurallarinin kaynagi.</summary>
    Task AddVitalAsync(Vital vital, CancellationToken ct = default);

    /// <summary>Crawl'in sayfalari, sayfalanmis.</summary>
    Task<IReadOnlyList<Page>> GetPagesAsync(Guid crawlId, int skip, int take, CancellationToken ct = default);

    Task<int> CountPagesAsync(Guid crawlId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
