using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities.Crawling;

public class Crawl
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SiteId { get; set; }
    public Site? Site { get; set; }

    public CrawlStatus Status { get; set; } = CrawlStatus.Queued;
    public CrawlTrigger Trigger { get; set; } = CrawlTrigger.Manual;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    public int PagesDiscovered { get; set; }
    public int PagesCrawled { get; set; }

    public decimal? OverallScore { get; set; }

    /// <summary>Kategori bazli skorlar: {"indexability":82.5,"meta":61.0,...}</summary>
    public Dictionary<string, decimal> CategoryScores { get; set; } = [];

    /// <summary>Sorun sayilari: {"critical":3,"high":12,"medium":40,"low":8}</summary>
    public Dictionary<string, int> IssueCounts { get; set; } = [];

    /// <summary>Skorlamada kullanilan agirliklar — gecmis crawl'lar bozulmasin diye dondurulur.</summary>
    public Dictionary<string, int> ScoringSnapshot { get; set; } = [];

    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Page> Pages { get; set; } = [];
    public ICollection<Issue> Issues { get; set; } = [];
}
