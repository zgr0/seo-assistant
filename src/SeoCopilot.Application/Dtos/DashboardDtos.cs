namespace SeoCopilot.Application.Dtos;

public record DashboardDto(
    int SiteCount,
    decimal? AverageScore,
    IReadOnlyDictionary<string, int> OpenIssueCounts,
    IReadOnlyList<DashboardSiteDto> Sites,
    IReadOnlyList<CrawlListItemDto> RecentCrawls,
    IReadOnlyList<ContentJobDto> RecentContentJobs);

/// <summary>Site karti — son crawl ozeti ve bir onceki crawl'a gore skor degisimi.</summary>
public record DashboardSiteDto(
    Guid SiteId,
    string Name,
    string BaseUrl,
    bool IsActive,
    Guid? LastCrawlId,
    string? LastCrawlStatus,
    decimal? LastScore,
    decimal? ScoreDelta,
    DateTimeOffset? LastCrawlAt,
    IReadOnlyDictionary<string, int> IssueCounts);
