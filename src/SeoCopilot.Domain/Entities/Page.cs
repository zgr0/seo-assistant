namespace SeoCopilot.Domain.Entities;

/// <summary>Bir crawl icinde taranan tek bir URL ve o sayfada bulunan findings.</summary>
public class Page
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CrawlId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public string? Title { get; private set; }
    public string? MetaDescription { get; private set; }

    private readonly List<Finding> _findings = [];
    public IReadOnlyCollection<Finding> Findings => _findings;

    private Page() { } // EF

    internal Page(Guid crawlId, string url)
    {
        CrawlId = crawlId;
        Url = url;
    }

    public void SetResponse(int statusCode, string? title, string? metaDescription)
    {
        StatusCode = statusCode;
        Title = title;
        MetaDescription = metaDescription;
    }

    public void AddFinding(Finding finding) => _findings.Add(finding);
}
