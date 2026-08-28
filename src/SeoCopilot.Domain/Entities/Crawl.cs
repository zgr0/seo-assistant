using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities;

/// <summary>Bir sitenin tek seferlik taranmasi. Sayfalari ve toplam skoru tutar.</summary>
public class Crawl
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SiteId { get; private set; }
    public CrawlStatus Status { get; private set; } = CrawlStatus.Queued;
    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; private set; }
    public int Score { get; private set; }
    public string? Error { get; private set; }

    private readonly List<Page> _pages = [];
    public IReadOnlyCollection<Page> Pages => _pages;

    private Crawl() { } // EF

    internal Crawl(Guid siteId) => SiteId = siteId;

    public void MarkRunning() => Status = CrawlStatus.Running;

    public Page AddPage(string url)
    {
        var page = new Page(Id, url);
        _pages.Add(page);
        return page;
    }

    public void Complete(int score)
    {
        Score = score;
        Status = CrawlStatus.Completed;
        FinishedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string error)
    {
        Error = error;
        Status = CrawlStatus.Failed;
        FinishedAt = DateTimeOffset.UtcNow;
    }
}
