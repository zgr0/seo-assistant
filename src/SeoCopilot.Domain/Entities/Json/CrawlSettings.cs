namespace SeoCopilot.Domain.Entities.Json;

/// <summary>sites.crawl_settings (jsonb) govdesi.</summary>
public sealed class CrawlSettings
{
    public int MaxPages { get; set; } = 500;
    public int MaxDepth { get; set; } = 5;
    public int DelayMs { get; set; } = 500;
    public bool RenderJs { get; set; }
    public int Concurrency { get; set; } = 3;

    /// <summary>
    /// Durum yoklamasi yapilacak azami ikili varlik (pdf, zip, jpg...) sayisi. Bunlar sayfa
    /// olarak taranmaz, <see cref="MaxPages"/> butcesinden dusmez. 0 → yoklama kapali.
    /// </summary>
    public int MaxAssetChecks { get; set; } = 200;

    public List<string> IncludePatterns { get; set; } = [];
    public List<string> ExcludePatterns { get; set; } = [];
}
