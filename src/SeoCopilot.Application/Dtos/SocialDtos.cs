namespace SeoCopilot.Application.Dtos;

/// <summary>
/// Site geneli sosyal medya paketi istegi. Her platform x secilen sayfa icin bir is acilir.
/// </summary>
/// <param name="ImageTemplates">
/// Gorsel tasarim sablonlari (<c>Overlay</c>, <c>Split</c>, <c>Framed</c>, <c>Label</c>, <c>Poster</c>,
/// <c>Quote</c>). Bos ya da null ise ayarlardaki liste (varsayilan: hepsi donusumlu) kullanilir.
/// </param>
public record CreateSocialKitRequest(
    Guid SiteId,
    List<string> PlatformCodes,
    int? PostCount = null,
    Guid? BrandProfileId = null,
    List<string>? ImageTemplates = null);

public record SocialKitResponse(
    Guid SiteId,
    Guid CrawlId,
    IReadOnlyList<Guid> JobIds,
    IReadOnlyList<string> PageUrls);
