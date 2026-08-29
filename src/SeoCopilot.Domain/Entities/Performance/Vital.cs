using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities.Performance;

/// <summary>Core Web Vitals olcumu (PSI lab veya field).</summary>
public class Vital
{
    public long Id { get; set; }

    public Guid SiteId { get; set; }
    public Site? Site { get; set; }

    public Guid? CrawlId { get; set; }
    public Crawl? Crawl { get; set; }

    public string Url { get; set; } = string.Empty;
    public VitalsDevice Device { get; set; }
    public VitalsSource Source { get; set; }

    public int? LcpMs { get; set; }
    public int? InpMs { get; set; }
    public decimal? Cls { get; set; }
    public int? TtfbMs { get; set; }
    public int? FcpMs { get; set; }
    public int? PerfScore { get; set; }

    public DateTimeOffset CollectedAt { get; set; } = DateTimeOffset.UtcNow;
}
