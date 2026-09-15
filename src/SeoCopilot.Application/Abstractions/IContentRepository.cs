using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Abstractions;

/// <summary>Daha once uretilmis sosyal gonderi — sayfa URL'i, platform ve varyant govdeleri.</summary>
/// <param name="PageUrl">Sayfa satiri silinmisse null; o durumda <c>input.pageUrl</c> okunur.</param>
public sealed record SocialPostRecord(
    Guid JobId, string? PageUrl, string Input, string? PlatformCode, IReadOnlyList<string> Bodies);

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

    /// <summary>
    /// Sitenin basarisiz olmayan sosyal paket isleri, yeniden eskiye (en fazla
    /// <paramref name="take"/>) — ayni sayfanin/metnin tekrar uretilmesini onlemek icin.
    /// </summary>
    Task<IReadOnlyList<SocialPostRecord>> ListSocialPostsAsync(
        Guid tenantId, Guid siteId, int take, CancellationToken ct = default);

    /// <summary>Isi ve (veritabani cascade'iyle) varyantlarini ve gorsel kayitlarini siler.</summary>
    Task DeleteContentJobAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Isin gorsellerinin depo anahtarlari — kayit silinmeden once dosyalar icin okunur.</summary>
    Task<IReadOnlyList<string>> ListAssetKeysForJobAsync(Guid jobId, CancellationToken ct = default);

    // --- uretilen gorseller ---

    Task AddContentAssetAsync(ContentAsset asset, CancellationToken ct = default);

    Task<ContentAsset?> GetAssetForTenantAsync(
        Guid assetId, Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Galeride gosterilecek gorseller, yeniden eskiye: yazili surumler ve yazili kopyasi
    /// olmayan ham gorseller (bir gorsel iki kez listelenmez). Is ve sayfa dahil.
    /// </summary>
    Task<(IReadOnlyList<ContentAsset> Items, int Total)> ListDisplayAssetsAsync(
        Guid tenantId, Guid? siteId, int skip, int take, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
