namespace SeoCopilot.Application.Dtos;

/// <summary>
/// Site geneli sosyal medya paketi istegi. Her platform x secilen sayfa icin bir is acilir.
/// </summary>
/// <param name="ImageTemplates">
/// Gorsel tasarim sablonlari (<c>Overlay</c>, <c>Split</c>, <c>Framed</c>, <c>Label</c>, <c>Poster</c>,
/// <c>Quote</c>). Bos ya da null ise ayarlardaki liste (varsayilan: hepsi donusumlu) kullanilir.
/// </param>
/// <param name="AiImages">
/// true: gorseller once yapay zekayla uretilir; basarisiz olursa site fotografina, o da yoksa
/// marka kartina dusulur. Gunluk sinira tabidir (<c>SocialImages:MaxAiImagesPerDay</c>).
/// </param>
public record CreateSocialKitRequest(
    Guid SiteId,
    List<string> PlatformCodes,
    int? PostCount = null,
    Guid? BrandProfileId = null,
    List<string>? ImageTemplates = null,
    bool? AiImages = null);

public record SocialKitResponse(
    Guid SiteId,
    Guid CrawlId,
    IReadOnlyList<Guid> JobIds,
    IReadOnlyList<string> PageUrls);

/// <summary>Formdaki "Yapay zeka ile uret" dugmesinin durumu.</summary>
/// <param name="AiEnabled">Cloudflare anahtari tanimli mi.</param>
/// <param name="UsedToday">Bugun (UTC) yapay zeka gorseli istenen is sayisi.</param>
public record SocialImageSettingsDto(bool AiEnabled, int DailyLimit, int UsedToday);
