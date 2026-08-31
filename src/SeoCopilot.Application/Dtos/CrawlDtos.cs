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

public record IssueDto(
    long Id,
    string RuleCode,
    string Severity,
    int Weight,
    string Status,
    Guid? PageId,
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
        i.Evidence.Found,
        i.Evidence.Expected,
        i.Evidence.SampleUrls);
}

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
