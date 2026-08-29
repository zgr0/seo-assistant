using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Sites;

namespace SeoCopilot.Application.Abstractions;

public interface ISiteRepository
{
    Task<Site?> GetSiteAsync(Guid siteId, CancellationToken ct = default);

    /// <summary>Crawl'i Pages + Issues ile birlikte getirir.</summary>
    Task<Crawl?> GetCrawlAsync(Guid crawlId, CancellationToken ct = default);

    Task AddCrawlAsync(Crawl crawl, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
