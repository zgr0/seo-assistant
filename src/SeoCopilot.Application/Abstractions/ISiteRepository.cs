using SeoCopilot.Domain.Entities;

namespace SeoCopilot.Application.Abstractions;

public interface ISiteRepository
{
    Task<Site?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Crawl?> GetCrawlAsync(Guid crawlId, CancellationToken ct = default);
    Task AddAsync(Site site, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
