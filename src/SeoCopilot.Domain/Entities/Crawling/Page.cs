namespace SeoCopilot.Domain.Entities.Crawling;

public class Page
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CrawlId { get; set; }
    public Crawl? Crawl { get; set; }

    public string Url { get; set; } = string.Empty;

    /// <summary>sha256(url) — b-tree index uzunluk limiti icin.</summary>
    public byte[] UrlHash { get; set; } = [];

    public int Depth { get; set; }
    public int StatusCode { get; set; }
    public string? ContentType { get; set; }
    public string? RedirectTo { get; set; }
    public int? ResponseTimeMs { get; set; }
    public int? HtmlSizeBytes { get; set; }

    public string? Title { get; set; }
    public int? TitleLength { get; set; }
    public string? MetaDescription { get; set; }
    public int? MetaDescLength { get; set; }

    public List<string> H1Texts { get; set; } = [];
    public int H2Count { get; set; }
    public int WordCount { get; set; }

    public string? CanonicalUrl { get; set; }
    public string? RobotsMeta { get; set; }

    /// <summary>Open Graph alanlari (jsonb).</summary>
    public string? OgData { get; set; }

    public List<string> SchemaTypes { get; set; } = [];

    public int ImagesTotal { get; set; }
    public int ImagesNoAlt { get; set; }

    /// <summary>Tarama sonunda hesaplanir.</summary>
    public int InlinkCount { get; set; }
    public int OutlinkInternal { get; set; }
    public int OutlinkExternal { get; set; }

    /// <summary>Duplicate content tespiti.</summary>
    public byte[]? ContentHash { get; set; }

    /// <summary>LLM ve sosyal medya uretimi icin ana icerik.</summary>
    public string? MainText { get; set; }

    public string? Lang { get; set; }
    public DateTimeOffset CrawledAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PageLink> OutLinks { get; set; } = [];
}
