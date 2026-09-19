using Microsoft.EntityFrameworkCore;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services.Social;
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
            .Include(j => j.Variants).ThenInclude(v => v.ImageAsset)
            .Include(j => j.Page)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId, ct);

    public async Task<(IReadOnlyList<ContentJob> Items, int Total)> ListContentJobsAsync(
        Guid tenantId, ContentJobType? type, string? platformCode, ContentJobStatus? status,
        Guid? siteId, Guid? pageId, int skip, int take, CancellationToken ct = default)
    {
        var q = db.ContentJobs
            .Include(j => j.Variants).ThenInclude(v => v.ImageAsset)
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

    public async Task<IReadOnlyList<SocialPostRecord>> ListSocialPostsAsync(
        Guid tenantId, Guid siteId, int take, CancellationToken ct = default)
    {
        var rows = await db.ContentJobs
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId
                && j.SiteId == siteId
                && j.Type == ContentJobType.SocialKit
                && j.Status != ContentJobStatus.Failed)
            .OrderByDescending(j => j.CreatedAt)
            .Take(take)
            .Select(j => new
            {
                j.Id,
                PageUrl = j.Page != null ? j.Page.Url : null,
                j.Input,
                j.PlatformCode,
                Bodies = j.Variants.Select(v => v.Body).ToList()
            })
            .ToListAsync(ct);

        return [.. rows.Select(r => new SocialPostRecord(r.Id, r.PageUrl, r.Input, r.PlatformCode, r.Bodies))];
    }

    /// <summary>jsonb <c>@&gt;</c> filtresi: <c>{"imageSource":"ai"}</c>.</summary>
    private static readonly string AiImageInput =
        $$"""{"{{SocialImageSettings.InputKey}}":"{{SocialImageSettings.AiSource}}"}""";

    public Task<int> CountAiImageJobsSinceAsync(
        Guid tenantId, DateTimeOffset since, CancellationToken ct = default) =>
        db.ContentJobs.CountAsync(j =>
            j.TenantId == tenantId
            && j.Type == ContentJobType.SocialKit
            && j.CreatedAt >= since
            && EF.Functions.JsonContains(j.Input, AiImageInput), ct);

    public Task DeleteContentJobAsync(Guid jobId, CancellationToken ct = default) =>
        db.ContentJobs.Where(j => j.Id == jobId).ExecuteDeleteAsync(ct);

    public async Task<IReadOnlyList<string>> ListAssetKeysForJobAsync(
        Guid jobId, CancellationToken ct = default) =>
        await db.ContentAssets
            .Where(a => a.JobId == jobId)
            .Select(a => a.StorageKey)
            .ToListAsync(ct);

    public async Task AddContentAssetAsync(ContentAsset asset, CancellationToken ct = default) =>
        await db.ContentAssets.AddAsync(asset, ct);

    public Task<ContentAsset?> GetAssetForTenantAsync(
        Guid assetId, Guid tenantId, CancellationToken ct = default) =>
        db.ContentAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.TenantId == tenantId, ct);

    public async Task<(IReadOnlyList<ContentAsset> Items, int Total)> ListDisplayAssetsAsync(
        Guid tenantId, Guid? siteId, int skip, int take, CancellationToken ct = default)
    {
        var q = db.ContentAssets.Where(a => a.TenantId == tenantId);

        if (siteId is Guid site) q = q.Where(a => a.Job!.SiteId == site);

        // Yazili kopyasi olan ham gorsel ayrica listelenmez — kopya zaten onu isaret eder.
        q = q.Where(a => a.Kind == ContentAssetKind.Captioned
            || !db.ContentAssets.Any(c => c.SourceAssetId == a.Id));

        var total = await q.CountAsync(ct);
        var items = await q
            .Include(a => a.Job!).ThenInclude(j => j.Page)
            .Include(a => a.Job!).ThenInclude(j => j.Variants)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip(skip)
            .Take(take)
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
