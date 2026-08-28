using SeoCopilot.Domain.Entities;

namespace SeoCopilot.Application.Dtos;

public record StartCrawlRequest(string Url, string OwnerEmail);

public record StartCrawlResponse(Guid SiteId, Guid CrawlId);

public record CrawlSummaryDto(
    Guid CrawlId,
    string Status,
    int Score,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    IReadOnlyList<PageDto> Pages)
{
    public static CrawlSummaryDto From(Crawl c) => new(
        c.Id,
        c.Status.ToString(),
        c.Score,
        c.StartedAt,
        c.FinishedAt,
        c.Pages.Select(PageDto.From).ToList());
}

public record PageDto(string Url, int StatusCode, string? Title, IReadOnlyList<FindingDto> Findings)
{
    public static PageDto From(Page p) => new(
        p.Url,
        p.StatusCode,
        p.Title,
        p.Findings.Select(f => new FindingDto(f.RuleCode, f.Severity.ToString(), f.Message)).ToList());
}

public record FindingDto(string RuleCode, string Severity, string Message);
