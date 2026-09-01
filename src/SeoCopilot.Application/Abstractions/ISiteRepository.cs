using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Performance;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Abstractions;

public interface ISiteRepository
{
    Task<Site?> GetSiteAsync(Guid siteId, CancellationToken ct = default);

    /// <summary>Kiraci sinirini uygular — baska tenant'in sitesi icin null doner.</summary>
    Task<Site?> GetSiteForTenantAsync(Guid siteId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<Site>> ListSitesAsync(Guid tenantId, CancellationToken ct = default);

    Task AddSiteAsync(Site site, CancellationToken ct = default);

    /// <summary>Siteyi ve bagli crawl/issue/rapor kayitlarini siler (cascade).</summary>
    Task RemoveSiteAsync(Site site, CancellationToken ct = default);

    /// <summary>Crawl'i Issues ile birlikte getirir (Pages haric — 500 satir olabilir).</summary>
    Task<Crawl?> GetCrawlAsync(Guid crawlId, CancellationToken ct = default);

    /// <summary>Kiraci sinirini uygular; crawl → site → tenant zinciri uzerinden.</summary>
    Task<Crawl?> GetCrawlForTenantAsync(Guid crawlId, Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Durumu dogrudan veritabanindan okur (izlenen entity'yi atlar) — calisan bir crawl'in
    /// iptal edilip edilmedigini worker icinden gormek icin.
    /// </summary>
    Task<CrawlStatus?> ReadCrawlStatusAsync(Guid crawlId, CancellationToken ct = default);

    Task<IReadOnlyList<Crawl>> ListCrawlsAsync(
        Guid siteId, int skip, int take, CancellationToken ct = default);

    Task<int> CountCrawlsAsync(Guid siteId, CancellationToken ct = default);

    /// <summary>Sitenin en son crawl'i (Issues dahil); hic yoksa null.</summary>
    Task<Crawl?> GetLatestCrawlAsync(Guid siteId, CancellationToken ct = default);

    /// <summary>Kiracinin tum sitelerindeki son crawl'lar — dashboard icin.</summary>
    Task<IReadOnlyList<Crawl>> ListRecentCrawlsForTenantAsync(
        Guid tenantId, int take, CancellationToken ct = default);

    Task AddCrawlAsync(Crawl crawl, CancellationToken ct = default);

    Task AddPagesAsync(IEnumerable<Page> pages, CancellationToken ct = default);

    Task AddPageLinksAsync(IEnumerable<PageLink> links, CancellationToken ct = default);

    /// <summary>PSI olcumu — performans kurallarinin kaynagi.</summary>
    Task AddVitalAsync(Vital vital, CancellationToken ct = default);

    /// <summary>Crawl'in sayfalari, sayfalanmis.</summary>
    Task<IReadOnlyList<Page>> GetPagesAsync(Guid crawlId, int skip, int take, CancellationToken ct = default);

    /// <summary>Filtreli + sayfalanmis sayfa listesi; toplam eslesme sayisiyla birlikte.</summary>
    Task<(IReadOnlyList<Page> Items, int Total)> QueryPagesAsync(
        Guid crawlId, PageQuery query, int skip, int take, CancellationToken ct = default);

    Task<int> CountPagesAsync(Guid crawlId, CancellationToken ct = default);

    /// <summary>Tek sayfa; kiraci sinirini page → crawl → site zinciri uzerinden uygular.</summary>
    Task<Page?> GetPageForTenantAsync(Guid pageId, Guid tenantId, CancellationToken ct = default);

    /// <summary>Sayfanin bulgulari (kural metni dahil).</summary>
    Task<IReadOnlyList<Issue>> ListIssuesForPageAsync(Guid pageId, CancellationToken ct = default);

    /// <summary>Filtreli + sayfalanmis bulgu listesi; toplam eslesme sayisiyla birlikte.</summary>
    Task<(IReadOnlyList<Issue> Items, int Total)> QueryIssuesAsync(
        Guid crawlId, IssueQuery query, int skip, int take, CancellationToken ct = default);

    /// <summary>Crawl'in tum bulgulari — kiyaslama icin (sayfa URL'leri dahil).</summary>
    Task<IReadOnlyList<Issue>> ListIssuesAsync(Guid crawlId, CancellationToken ct = default);

    Task<Issue?> GetIssueForTenantAsync(long issueId, Guid tenantId, CancellationToken ct = default);

    /// <summary>Sitedeki ayni kurala ait acik bulgular — "tum siteye uygula" icin.</summary>
    Task<IReadOnlyList<Issue>> ListSiteIssuesByRuleAsync(
        Guid siteId, string ruleCode, CancellationToken ct = default);

    Task AddIssueIgnoreAsync(IssueIgnore ignore, CancellationToken ct = default);

    Task<IssueIgnore?> FindIssueIgnoreAsync(
        Guid siteId, string ruleCode, string? urlPattern, CancellationToken ct = default);

    Task RemoveIssueIgnoreAsync(IssueIgnore ignore, CancellationToken ct = default);

    /// <summary>Sitenin son olcumleri, yeniden eskiye.</summary>
    Task<IReadOnlyList<Vital>> ListVitalsAsync(
        Guid siteId, string? url, int take, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
