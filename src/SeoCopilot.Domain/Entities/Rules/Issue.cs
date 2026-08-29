using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Json;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities.Rules;

public class Issue
{
    public long Id { get; set; }

    public Guid CrawlId { get; set; }
    public Crawl? Crawl { get; set; }

    /// <summary>Denormalize — site geneli sorgu hizi icin.</summary>
    public Guid SiteId { get; set; }
    public Site? Site { get; set; }

    /// <summary>null = site geneli sorun.</summary>
    public Guid? PageId { get; set; }
    public Page? Page { get; set; }

    public string RuleCode { get; set; } = string.Empty;
    public Rule? Rule { get; set; }

    /// <summary>rules'tan kopyalanir (kural sonradan degisse de gecmis sabit kalir).</summary>
    public Severity Severity { get; set; }
    public int Weight { get; set; }

    public IssueEvidence Evidence { get; set; } = new();

    public IssueStatus Status { get; set; } = IssueStatus.Open;

    public Guid? FirstSeenCrawlId { get; set; }
    public Guid? ResolvedCrawlId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
