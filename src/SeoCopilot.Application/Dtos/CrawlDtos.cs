using SeoCopilot.Domain.Entities.Crawling;

namespace SeoCopilot.Application.Dtos;

public record StartCrawlRequest(Guid SiteId);

public record StartCrawlResponse(Guid CrawlId);

/// <summary>Sayfalanmis liste zarfi.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int Size);

public record CrawlSummaryDto(
    Guid CrawlId,
    Guid SiteId,
    string Status,
    decimal? OverallScore,
    int PagesDiscovered,
    int PagesCrawled,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? ErrorMessage,
    IReadOnlyDictionary<string, decimal> CategoryScores,
    IReadOnlyDictionary<string, int> IssueCounts,
    IReadOnlyList<IssueDto> Issues)
{
    /// <summary>Ozette tasinan azami bulgu sayisi; tamami icin /api/crawls/{id}/issues.</summary>
    public const int MaxIssuesInSummary = 100;

    public static CrawlSummaryDto From(Crawl c) => new(
        c.Id,
        c.SiteId,
        c.Status.ToString(),
        c.OverallScore,
        c.PagesDiscovered,
        c.PagesCrawled,
        c.StartedAt,
        c.FinishedAt,
        c.ErrorMessage,
        c.CategoryScores,
        c.IssueCounts,
        [.. c.Issues
            .OrderByDescending(i => i.Severity)
            .Take(MaxIssuesInSummary)
            .Select(IssueDto.From)]);
}

/// <summary>Crawl listesi satiri — bulgu detayi tasimaz.</summary>
public record CrawlListItemDto(
    Guid CrawlId,
    Guid SiteId,
    string Status,
    string Trigger,
    decimal? OverallScore,
    int PagesDiscovered,
    int PagesCrawled,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? ErrorMessage,
    IReadOnlyDictionary<string, int> IssueCounts,
    DateTimeOffset CreatedAt)
{
    public static CrawlListItemDto From(Crawl c) => new(
        c.Id,
        c.SiteId,
        c.Status.ToString(),
        c.Trigger.ToString(),
        c.OverallScore,
        c.PagesDiscovered,
        c.PagesCrawled,
        c.StartedAt,
        c.FinishedAt,
        c.ErrorMessage,
        c.IssueCounts,
        c.CreatedAt);
}

public record IssueDto(
    long Id,
    string RuleCode,
    string Severity,
    int Weight,
    string Status,
    Guid? PageId,
    string? PageUrl,
    string? Category,
    string? RuleTitle,
    string? HowToFix,
    string? Found,
    string? Expected,
    IReadOnlyList<string> SampleUrls)
{
    public static IssueDto From(Domain.Entities.Rules.Issue i) => new(
        i.Id,
        i.RuleCode,
        i.Severity.ToString(),
        i.Weight,
        i.Status.ToString(),
        i.PageId,
        i.Page?.Url,
        i.Rule?.Category.ToString(),
        i.Rule?.TitleTr,
        i.Rule?.HowToFixTr,
        i.Evidence.Found,
        i.Evidence.Expected,
        i.Evidence.SampleUrls);
}

/// <summary>Iki crawl arasindaki fark — FE'nin "onceki taramaya gore" panosu.</summary>
public record CrawlCompareDto(
    Guid CurrentCrawlId,
    Guid PreviousCrawlId,
    decimal? CurrentScore,
    decimal? PreviousScore,
    decimal? ScoreDelta,
    int CurrentPagesCrawled,
    int PreviousPagesCrawled,
    IReadOnlyDictionary<string, int> IssueCountDelta,
    IReadOnlyDictionary<string, decimal> CategoryScoreDelta,
    IReadOnlyList<IssueDto> NewIssues,
    IReadOnlyList<IssueDto> ResolvedIssues,
    int UnchangedIssueCount);

public record PageDto(
    Guid Id,
    string Url,
    int Depth,
    int StatusCode,
    string? ContentType,
    string? RedirectTo,
    int? ResponseTimeMs,
    int? HtmlSizeBytes,
    string? Title,
    int? TitleLength,
    string? MetaDescription,
    int? MetaDescLength,
    IReadOnlyList<string> H1Texts,
    int H2Count,
    int WordCount,
    string? CanonicalUrl,
    string? RobotsMeta,
    IReadOnlyList<string> SchemaTypes,
    int ImagesTotal,
    int ImagesNoAlt,
    int InlinkCount,
    int OutlinkInternal,
    int OutlinkExternal,
    string? Lang,
    DateTimeOffset CrawledAt)
{
    public static PageDto From(Page p) => new(
        p.Id,
        p.Url,
        p.Depth,
        p.StatusCode,
        p.ContentType,
        p.RedirectTo,
        p.ResponseTimeMs,
        p.HtmlSizeBytes,
        p.Title,
        p.TitleLength,
        p.MetaDescription,
        p.MetaDescLength,
        p.H1Texts,
        p.H2Count,
        p.WordCount,
        p.CanonicalUrl,
        p.RobotsMeta,
        p.SchemaTypes,
        p.ImagesTotal,
        p.ImagesNoAlt,
        p.InlinkCount,
        p.OutlinkInternal,
        p.OutlinkExternal,
        p.Lang,
        p.CrawledAt);
}

/// <summary>Tek sayfa detayi — liste DTO'suna ana metin, OG verisi ve bulgular eklenir.</summary>
public record PageDetailDto(
    PageDto Page,
    Guid CrawlId,
    string? OgData,
    string? MainText,
    IReadOnlyList<IssueDto> Issues)
{
    /// <summary>Detayda dondurulen azami ana metin uzunlugu.</summary>
    public const int MaxMainTextChars = 20_000;

    public static PageDetailDto From(Page p, IEnumerable<Domain.Entities.Rules.Issue> issues) => new(
        PageDto.From(p),
        p.CrawlId,
        p.OgData,
        p.MainText is { Length: > MaxMainTextChars } text ? text[..MaxMainTextChars] : p.MainText,
        [.. issues.OrderByDescending(i => i.Severity).Select(IssueDto.From)]);
}
