using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Abstractions;

/// <summary>Marka profilleri, platform seed'i ve icerik uretim isleri.</summary>
public interface IContentRepository
{
    // --- brand profiles ---

    Task<IReadOnlyList<BrandProfile>> ListBrandProfilesAsync(
        Guid tenantId, Guid? siteId, CancellationToken ct = default);

    Task<BrandProfile?> GetBrandProfileAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task AddBrandProfileAsync(BrandProfile profile, CancellationToken ct = default);

    /// <summary>Ayni kiraci (ve site) icindeki diger profillerin varsayilan isaretini kaldirir.</summary>
    Task ClearDefaultBrandProfilesAsync(
        Guid tenantId, Guid? siteId, Guid exceptId, CancellationToken ct = default);

    // --- platform profiles ---

    Task<IReadOnlyList<PlatformProfile>> ListPlatformProfilesAsync(
        bool onlyActive, CancellationToken ct = default);

    Task<PlatformProfile?> GetPlatformProfileAsync(string code, CancellationToken ct = default);

    // --- content jobs ---

    Task AddContentJobAsync(ContentJob job, CancellationToken ct = default);

    /// <summary>Varyantlar, marka profili, platform ve sayfa dahil.</summary>
    Task<ContentJob?> GetContentJobAsync(Guid jobId, CancellationToken ct = default);

    Task<ContentJob?> GetContentJobForTenantAsync(
        Guid jobId, Guid tenantId, CancellationToken ct = default);

    Task<(IReadOnlyList<ContentJob> Items, int Total)> ListContentJobsAsync(
        Guid tenantId, ContentJobType? type, string? platformCode, ContentJobStatus? status,
        Guid? siteId, Guid? pageId, int skip, int take, CancellationToken ct = default);

    /// <summary>CSV disa aktarimi icin — varyantlariyla birlikte, sayfalama yok.</summary>
    Task<IReadOnlyList<ContentJob>> GetContentJobsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> jobIds, CancellationToken ct = default);

    Task<ContentVariant?> GetVariantForTenantAsync(
        Guid variantId, Guid tenantId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
