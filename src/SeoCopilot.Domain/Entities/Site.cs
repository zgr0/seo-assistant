namespace SeoCopilot.Domain.Entities;

/// <summary>Taranan bir web sitesi (kok domain).</summary>
public class Site
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Url { get; private set; } = string.Empty;
    public string OwnerEmail { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private readonly List<Crawl> _crawls = [];
    public IReadOnlyCollection<Crawl> Crawls => _crawls;

    private Site() { } // EF

    public Site(string url, string ownerEmail)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url bos olamaz", nameof(url));
        Url = url.TrimEnd('/');
        OwnerEmail = ownerEmail;
    }

    public Crawl StartCrawl()
    {
        var crawl = new Crawl(Id);
        _crawls.Add(crawl);
        return crawl;
    }
}
