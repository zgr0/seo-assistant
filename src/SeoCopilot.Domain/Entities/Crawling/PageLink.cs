namespace SeoCopilot.Domain.Entities.Crawling;

public class PageLink
{
    public long Id { get; set; }
    public Guid CrawlId { get; set; }

    public Guid FromPageId { get; set; }
    public Page? FromPage { get; set; }

    public string ToUrl { get; set; } = string.Empty;
    public Guid? ToPageId { get; set; }
    public Page? ToPage { get; set; }

    public string? AnchorText { get; set; }
    public bool IsInternal { get; set; }
    public bool IsNofollow { get; set; }
}
