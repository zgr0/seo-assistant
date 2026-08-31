using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Abstractions;

/// <summary>
/// Kural motoruna koprü. SeoCopilot.Rules saf kalsin diye arayuz burada tanimli,
/// adapter Api katmaninda kayitli.
/// </summary>
public interface IRuleRunner
{
    /// <summary>Verilen sayfa icin bulgularin listesini ve 0-100 sayfa skorunu doner.</summary>
    RuleRunOutcome Run(ExtractedPage page);

    /// <summary>Tek sayfaya bakarak karar verilemeyen kurallar (yinelenen icerik, kirik ic link, sitemap...).</summary>
    IReadOnlyList<CrawlRuleFinding> RunCrawl(
        IReadOnlyList<CrawlPageFacts> pages,
        IReadOnlyList<CrawlLinkFacts> links,
        CrawlSiteFacts site);

    /// <summary>Crawl geneli skor: sayfa skorlarinin ortalamasi eksi crawl seviyesi cezalar.</summary>
    decimal OverallScore(IEnumerable<int> pageScores, IEnumerable<RuleFinding> crawlFindings);

    /// <summary>crawl.category_scores govdesi.</summary>
    Dictionary<string, decimal> CategoryScores(IEnumerable<RuleFinding> allFindings, int pageCount);

    /// <summary>crawl.scoring_snapshot govdesi — kullanilan ceza tablosu.</summary>
    Dictionary<string, int> ScoringSnapshot();
}

public record RuleRunOutcome(int Score, IReadOnlyList<RuleFinding> Findings);

public record RuleFinding(
    string RuleCode,
    RuleCategory Category,
    Severity Severity,
    int Weight,
    string Message)
{
    public IReadOnlyList<string> SampleUrls { get; init; } = [];
}

/// <summary>Crawl seviyesi kurallara girdi — taranmis tek sayfa.</summary>
public record CrawlPageFacts(Guid PageId, string Url, int StatusCode, byte[]? ContentHash)
{
    public int Depth { get; init; }
    public int InlinkCount { get; init; }
    public string? Title { get; init; }
    public string? MetaDescription { get; init; }
    public bool IsHome { get; init; }
    public bool NoIndex { get; init; }
}

/// <summary>Crawl seviyesi kurallara girdi — tek link. Hedef taranmadiysa TargetStatusCode null.</summary>
public record CrawlLinkFacts(Guid FromPageId, string FromUrl, string ToUrl, bool IsInternal, int? TargetStatusCode);

/// <summary>Crawl seviyesi kurallara girdi — siteye ait gercekler (sitemap, robots, vitals).</summary>
public record CrawlSiteFacts
{
    public bool SitemapFound { get; init; }
    public IReadOnlyCollection<string> SitemapUrls { get; init; } = [];
    public IReadOnlyCollection<string> BlockedUrls { get; init; } = [];
    public VitalsFacts? Vitals { get; init; }
    public Guid? HomePageId { get; init; }
}

/// <summary>PSI'dan alinan Core Web Vitals; alinamayan metrik null.</summary>
public record VitalsFacts(double? LcpMs, double? Cls, double? InpMs);

/// <summary>Crawl seviyesi bulgu ve baglandigi sayfa (null → site seviyesi).</summary>
public record CrawlRuleFinding(Guid? PageId, RuleFinding Finding);
