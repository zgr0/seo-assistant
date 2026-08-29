using Microsoft.EntityFrameworkCore;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Sites;

namespace SeoCopilot.Infrastructure.Persistence;

public sealed class SiteRepository(SeoCopilotDbContext db) : ISiteRepository
{
    public Task<Site?> GetSiteAsync(Guid siteId, CancellationToken ct = default) =>
        db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, ct);

    public Task<Crawl?> GetCrawlAsync(Guid crawlId, CancellationToken ct = default) =>
        db.Crawls
            .Include(c => c.Pages)
            .Include(c => c.Issues)
            .FirstOrDefaultAsync(c => c.Id == crawlId, ct);

    public async Task AddCrawlAsync(Crawl crawl, CancellationToken ct = default) =>
        await db.Crawls.AddAsync(crawl, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
