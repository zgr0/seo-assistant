using SeoCopilot.Domain.Entities.Crawling;

namespace SeoCopilot.Application.Dtos;

public record StartCrawlRequest(Guid SiteId);

public record StartCrawlResponse(Guid CrawlId);

public record CrawlSummaryDto(
    Guid CrawlId,
    Guid SiteId,
    string Status,
    decimal? OverallScore,
    int PagesCrawled,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    IReadOnlyDictionary<string, int> IssueCounts,
    IReadOnlyList<IssueDto> Issues)
{
    public static CrawlSummaryDto From(Crawl c) => new(
        c.Id,
        c.SiteId,
        c.Status.ToString(),
        c.OverallScore,
        c.PagesCrawled,
        c.StartedAt,
        c.FinishedAt,
        c.IssueCounts,
        c.Issues.Select(i => new IssueDto(
            i.RuleCode,
            i.Severity.ToString(),
            i.Weight,
            i.Status.ToString(),
            i.Evidence.Found,
            i.Evidence.Expected)).ToList());
}

public record IssueDto(
    string RuleCode,
    string Severity,
    int Weight,
    string Status,
    string? Found,
    string? Expected);
