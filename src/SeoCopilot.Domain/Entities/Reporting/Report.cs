using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities.Reporting;

public class Report
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SiteId { get; set; }
    public Site? Site { get; set; }

    public Guid CrawlId { get; set; }
    public Crawl? Crawl { get; set; }

    /// <summary>Onceki crawl ile kiyas raporu icin.</summary>
    public Guid? CompareCrawlId { get; set; }

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    /// <summary>Obje deposundaki dosya anahtari (PDF vb.).</summary>
    public string? StorageKey { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Queued;
    public DateTimeOffset? GeneratedAt { get; set; }
}
