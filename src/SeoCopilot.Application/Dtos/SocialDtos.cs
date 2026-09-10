namespace SeoCopilot.Application.Dtos;

/// <summary>
/// Site geneli sosyal medya paketi istegi. Her platform x secilen sayfa icin bir is acilir.
/// </summary>
public record CreateSocialKitRequest(
    Guid SiteId,
    List<string> PlatformCodes,
    int? PostCount = null,
    Guid? BrandProfileId = null);

public record SocialKitResponse(
    Guid SiteId,
    Guid CrawlId,
    IReadOnlyList<Guid> JobIds,
    IReadOnlyList<string> PageUrls);
