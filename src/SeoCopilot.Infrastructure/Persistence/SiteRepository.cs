using Microsoft.EntityFrameworkCore;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Domain.Entities;

namespace SeoCopilot.Infrastructure.Persistence;

public sealed class SiteRepository(SeoCopilotDbContext db) : ISiteRepository
{
    public Task<Site?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Sites.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Crawl?> GetCrawlAsync(Guid crawlId, CancellationToken ct = default) =>
        db.Crawls
            .Include(c => c.Pages)
            .ThenInclude(p => p.Findings)
            .FirstOrDefaultAsync(c => c.Id == crawlId, ct);

    public async Task AddAsync(Site site, CancellationToken ct = default) =>
        await db.Sites.AddAsync(site, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
