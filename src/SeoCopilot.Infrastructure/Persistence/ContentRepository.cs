using Microsoft.EntityFrameworkCore;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Infrastructure.Persistence;

public sealed class ContentRepository(SeoCopilotDbContext db) : IContentRepository
{
    public async Task<IReadOnlyList<BrandProfile>> ListBrandProfilesAsync(
        Guid tenantId, Guid? siteId, CancellationToken ct = default)
    {
        var q = db.BrandProfiles.Where(p => p.TenantId == tenantId);

        // Site verildiyse o siteye ozel profiller + kiraci geneli (site_id null) profiller.
        if (siteId is Guid id) q = q.Where(p => p.SiteId == id || p.SiteId == null);

        return await q
            .OrderByDescending(p => p.IsDefault)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<BrandProfile?> GetBrandProfileAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        db.BrandProfiles.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

    public async Task AddBrandProfileAsync(BrandProfile profile, CancellationToken ct = default) =>
        await db.BrandProfiles.AddAsync(profile, ct);

    public async Task ClearDefaultBrandProfilesAsync(
        Guid tenantId, Guid? siteId, Guid exceptId, CancellationToken ct = default)
    {
        var others = await db.BrandProfiles
            .Where(p => p.TenantId == tenantId && p.SiteId == siteId && p.IsDefault && p.Id != exceptId)
            .ToListAsync(ct);

        foreach (var profile in others)
            profile.IsDefault = false;
    }

    public async Task<IReadOnlyList<PlatformProfile>> ListPlatformProfilesAsync(
        bool onlyActive, CancellationToken ct = default)
    {
        var q = db.PlatformProfiles.AsQueryable();
        if (onlyActive) q = q.Where(p => p.IsActive);
        return await q.OrderBy(p => p.Code).ToListAsync(ct);
    }

    public Task<PlatformProfile?> GetPlatformProfileAsync(string code, CancellationToken ct = default) =>
        db.PlatformProfiles.FirstOrDefaultAsync(p => p.Code == code, ct);

    public async Task AddContentJobAsync(ContentJob job, CancellationToken ct = default) =>
        await db.ContentJobs.AddAsync(job, ct);

    public Task<ContentJob?> GetContentJobAsync(Guid jobId, CancellationToken ct = default) =>
        db.ContentJobs
            .Include(j => j.Variants)
            .Include(j => j.BrandProfile)
            .Include(j => j.Page)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

    public Task<ContentJob?> GetContentJobForTenantAsync(
        Guid jobId, Guid tenantId, CancellationToken ct = default) =>
        db.ContentJobs
            .Include(j => j.Variants)
            .Include(j => j.Page)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId, ct);

    public async Task<(IReadOnlyList<ContentJob> Items, int Total)> ListContentJobsAsync(
        Guid tenantId, ContentJobType? type, string? platformCode, ContentJobStatus? status,
        Guid? siteId, Guid? pageId, int skip, int take, CancellationToken ct = default)
    {
        var q = db.ContentJobs
            .Include(j => j.Variants)
            .Include(j => j.Page)
            .Where(j => j.TenantId == tenantId);

        if (type is ContentJobType jobType) q = q.Where(j => j.Type == jobType);
        if (platformCode is not null) q = q.Where(j => j.PlatformCode == platformCode);
        if (status is ContentJobStatus jobStatus) q = q.Where(j => j.Status == jobStatus);
        if (siteId is Guid site) q = q.Where(j => j.SiteId == site);
        if (pageId is Guid page) q = q.Where(j => j.PageId == page);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(j => j.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<ContentJob>> GetContentJobsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> jobIds, CancellationToken ct = default) =>
        await db.ContentJobs
            .Include(j => j.Variants)
            .Include(j => j.Page)
            .Where(j => j.TenantId == tenantId && jobIds.Contains(j.Id))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    public Task<ContentVariant?> GetVariantForTenantAsync(
        Guid variantId, Guid tenantId, CancellationToken ct = default) =>
        db.ContentVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.Job!.TenantId == tenantId, ct);

    public async Task AddContentAssetAsync(ContentAsset asset, CancellationToken ct = default) =>
        await db.ContentAssets.AddAsync(asset, ct);

    public Task<ContentAsset?> GetAssetForTenantAsync(
        Guid assetId, Guid tenantId, CancellationToken ct = default) =>
        db.ContentAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.TenantId == tenantId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
