using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Rules;

/// <summary>Crawl seviyesi kural girdisi — tek sayfaya bakarak karar verilemeyen kurallar icin.</summary>
public sealed record CrawlPageInput(Guid PageId, string Url, int StatusCode, byte[]? ContentHash)
{
    /// <summary>Kok sayfadan uzaklik; TOO_DEEP bunu olcer.</summary>
    public int Depth { get; init; }

    /// <summary>Bu sayfaya isaret eden ic link sayisi; 0 ise ORPHAN_PAGE adayi.</summary>
    public int InlinkCount { get; init; }

    public string? Title { get; init; }
    public string? MetaDescription { get; init; }

    /// <summary>Site kok sayfasi mi — orphan sayilmaz.</summary>
    public bool IsHome { get; init; }

    /// <summary>robots meta / X-Robots-Tag noindex tasiyor mu.</summary>
    public bool NoIndex { get; init; }

    /// <summary>Dizine girmesi beklenen sayfa mi: 2xx ve noindex yok.</summary>
    public bool IsIndexable => StatusCode is >= 200 and < 300 && !NoIndex;
}

/// <summary>Hedefi taranmamis linklerde <paramref name="TargetStatusCode"/> null gelir; o link degerlendirilmez.</summary>
public sealed record CrawlLinkInput(Guid FromPageId, string FromUrl, string ToUrl, bool IsInternal, int? TargetStatusCode);

/// <summary>Core Web Vitals olcumu (PSI). Alinamayan metrik null gelir ve degerlendirilmez.</summary>
public sealed record VitalsInput(double? LcpMs, double? Cls, double? InpMs);

/// <summary>Tek sayfaya degil siteye ait gercekler.</summary>
public sealed record CrawlSiteInput
{
    /// <summary>robots.txt'de bildirilen ya da /sitemap.xml'de bulunan bir sitemap okunabildi mi.</summary>
    public bool SitemapFound { get; init; }

    /// <summary>Sitemap'te gecen URL'ler (kanonik mutlak hal).</summary>
    public IReadOnlyCollection<string> SitemapUrls { get; init; } = [];

    /// <summary>robots.txt yuzunden taranamayan URL'ler.</summary>
    public IReadOnlyCollection<string> BlockedUrls { get; init; } = [];

    public VitalsInput? Vitals { get; init; }

    /// <summary>Site seviyesi bulgularin baglanacagi sayfa; yoksa bulgu sayfasiz yazilir.</summary>
    public Guid? HomePageId { get; init; }
}

/// <summary>Bir crawl seviyesi bulgu ve baglandigi sayfa (yoksa null → site seviyesi).</summary>
public sealed record CrawlViolation(Guid? PageId, RuleViolation Finding);

/// <summary>
/// Tum crawl'a bakarak calisan kurallar. Saf ve deterministik — sayfa/link/site kumesinden
/// bulgu uretir, hicbir yan etkisi yoktur.
/// </summary>
public static class CrawlRules
{
    private const int MaxSampleUrls = 10;

    /// <summary>TOO_DEEP esigi — kok sayfadan bu kadar tiklamadan uzak sayfalar bulgu uretir.</summary>
    public const int MaxDepth = 4;

    public const double PoorLcpMs = 4000;
    public const double PoorCls = 0.25;
    public const double PoorInpMs = 500;

    public static IReadOnlyList<CrawlViolation> Evaluate(
        IReadOnlyList<CrawlPageInput> pages,
        IReadOnlyList<CrawlLinkInput> links,
        CrawlSiteInput? site = null)
    {
        var result = new List<CrawlViolation>();
        result.AddRange(DuplicateContent(pages));
        result.AddRange(DuplicateTitles(pages));
        result.AddRange(DuplicateDescriptions(pages));
        result.AddRange(BrokenInternalLinks(links));
        result.AddRange(OrphanPages(pages));
        result.AddRange(TooDeepPages(pages));

        if (site is not null)
        {
            result.AddRange(SitemapFindings(pages, site));
            result.AddRange(BlockedByRobots(site));
            result.AddRange(PoorVitals(site));
        }

        return result;
    }

    /// <summary>DUPLICATE_CONTENT — ayni content_hash birden fazla sayfada.</summary>
    private static IEnumerable<CrawlViolation> DuplicateContent(IReadOnlyList<CrawlPageInput> pages)
    {
        var groups = pages
            .Where(p => p.ContentHash is { Length: > 0 } && p.StatusCode is >= 200 and < 300)
            .GroupBy(p => Convert.ToHexString(p.ContentHash!))
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();

            yield return new CrawlViolation(
                members[0].PageId,
                new RuleViolation(
                    "DUPLICATE_CONTENT",
                    RuleCategory.Content,
                    Severity.High,
                    7,
                    $"{members.Count} sayfa aynı içeriğe sahip.")
                {
                    SampleUrls = Samples(members)
                });
        }
    }

    /// <summary>META_TITLE_DUPLICATE — ayni title birden fazla dizinlenebilir sayfada.</summary>
    private static IEnumerable<CrawlViolation> DuplicateTitles(IReadOnlyList<CrawlPageInput> pages) =>
        DuplicateText(
            pages, p => p.Title,
            "META_TITLE_DUPLICATE", RuleCategory.Meta, Severity.Medium, 5,
            (count, value) => $"{count} sayfa aynı title'ı kullanıyor: \"{value}\".");

    /// <summary>META_DESC_DUPLICATE — ayni meta description birden fazla dizinlenebilir sayfada.</summary>
    private static IEnumerable<CrawlViolation> DuplicateDescriptions(IReadOnlyList<CrawlPageInput> pages) =>
        DuplicateText(
            pages, p => p.MetaDescription,
            "META_DESC_DUPLICATE", RuleCategory.Meta, Severity.Low, 3,
            (count, value) => $"{count} sayfa aynı meta description'ı kullanıyor: \"{Clip(value)}\".");

    private static IEnumerable<CrawlViolation> DuplicateText(
        IReadOnlyList<CrawlPageInput> pages,
        Func<CrawlPageInput, string?> selector,
        string code, RuleCategory category, Severity severity, int weight,
        Func<int, string, string> message)
    {
        var groups = pages
            .Where(p => p.IsIndexable && !string.IsNullOrWhiteSpace(selector(p)))
            .GroupBy(p => selector(p)!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();

            yield return new CrawlViolation(
                members[0].PageId,
                new RuleViolation(code, category, severity, weight, message(members.Count, group.Key))
                {
                    SampleUrls = Samples(members)
                });
        }
    }

    /// <summary>BROKEN_INTERNAL_LINK — 4xx/5xx donen bir sayfaya giden ic link. Kaynak sayfa basina tek bulgu.</summary>
    private static IEnumerable<CrawlViolation> BrokenInternalLinks(IReadOnlyList<CrawlLinkInput> links)
    {
        var broken = links.Where(l =>
            l.IsInternal &&
            l.TargetStatusCode is int code &&
            (code == 0 || code >= 400));

        foreach (var group in broken.GroupBy(l => l.FromPageId))
        {
            var members = group.ToList();
            yield return new CrawlViolation(
                group.Key,
                new RuleViolation(
                    "BROKEN_INTERNAL_LINK",
                    RuleCategory.Links,
                    Severity.High,
                    6,
                    $"{members[0].FromUrl} sayfasında {members.Count} kırık iç link var.")
                {
                    SampleUrls = [.. members.Select(l => l.ToUrl).Take(MaxSampleUrls)]
                });
        }
    }

    /// <summary>ORPHAN_PAGE — hicbir ic link isaret etmiyor. Kok sayfa haric.</summary>
    private static IEnumerable<CrawlViolation> OrphanPages(IReadOnlyList<CrawlPageInput> pages) =>
        pages
            .Where(p => p.IsIndexable && !p.IsHome && p.InlinkCount == 0)
            .Select(p => new CrawlViolation(
                p.PageId,
                new RuleViolation(
                    "ORPHAN_PAGE",
                    RuleCategory.Links,
                    Severity.Medium,
                    4,
                    "Sayfaya hiçbir iç link işaret etmiyor.")
                {
                    SampleUrls = [p.Url]
                }));

    /// <summary>TOO_DEEP — kok sayfadan 4 tiklamadan uzak.</summary>
    private static IEnumerable<CrawlViolation> TooDeepPages(IReadOnlyList<CrawlPageInput> pages) =>
        pages
            .Where(p => p.IsIndexable && p.Depth > MaxDepth)
            .Select(p => new CrawlViolation(
                p.PageId,
                new RuleViolation(
                    "TOO_DEEP",
                    RuleCategory.Links,
                    Severity.Low,
                    3,
                    $"Sayfa kök sayfadan {p.Depth} tıklama uzakta (en fazla {MaxDepth} olmalı).")
                {
                    SampleUrls = [p.Url]
                }));

    /// <summary>SITEMAP_MISSING / PAGE_NOT_IN_SITEMAP.</summary>
    private static IEnumerable<CrawlViolation> SitemapFindings(
        IReadOnlyList<CrawlPageInput> pages, CrawlSiteInput site)
    {
        if (!site.SitemapFound)
        {
            yield return new CrawlViolation(
                site.HomePageId,
                new RuleViolation(
                    "SITEMAP_MISSING",
                    RuleCategory.Indexability,
                    Severity.Medium,
                    5,
                    "Sitede okunabilir bir sitemap bulunamadı."));

            // Sitemap yoksa her sayfanin disarida kalmasi beklenir; ayrica bulgu uretmeyiz.
            yield break;
        }

        var known = new HashSet<string>(site.SitemapUrls.Select(TrimSlash), StringComparer.OrdinalIgnoreCase);

        foreach (var page in pages.Where(p => p.IsIndexable && !known.Contains(TrimSlash(p.Url))))
        {
            yield return new CrawlViolation(
                page.PageId,
                new RuleViolation(
                    "PAGE_NOT_IN_SITEMAP",
                    RuleCategory.Indexability,
                    Severity.Low,
                    3,
                    "Sayfa sitemap'te listelenmiyor.")
                {
                    SampleUrls = [page.Url]
                });
        }
    }

    /// <summary>BLOCKED_BY_ROBOTS_TXT — robots.txt taranmasi gereken adresleri kapatiyor.</summary>
    private static IEnumerable<CrawlViolation> BlockedByRobots(CrawlSiteInput site)
    {
        if (site.BlockedUrls.Count == 0) yield break;

        yield return new CrawlViolation(
            site.HomePageId,
            new RuleViolation(
                "BLOCKED_BY_ROBOTS_TXT",
                RuleCategory.Indexability,
                Severity.High,
                7,
                $"robots.txt {site.BlockedUrls.Count} iç adresin taranmasını engelliyor.")
            {
                SampleUrls = [.. site.BlockedUrls.Take(MaxSampleUrls)]
            });
    }

    /// <summary>LCP_POOR / CLS_POOR / INP_POOR — PSI olcumleri "poor" esiginin uzerinde.</summary>
    private static IEnumerable<CrawlViolation> PoorVitals(CrawlSiteInput site)
    {
        if (site.Vitals is not { } vitals) yield break;

        if (vitals.LcpMs > PoorLcpMs)
            yield return Vital("LCP_POOR", Severity.High, 7,
                $"LCP {vitals.LcpMs / 1000:0.0} sn — {PoorLcpMs / 1000:0.0} sn esiginin uzerinde.");

        if (vitals.Cls > PoorCls)
            yield return Vital("CLS_POOR", Severity.Medium, 5,
                $"CLS {vitals.Cls:0.000} — {PoorCls:0.00} esiginin uzerinde.");

        if (vitals.InpMs > PoorInpMs)
            yield return Vital("INP_POOR", Severity.Medium, 5,
                $"INP {vitals.InpMs:0} ms — {PoorInpMs:0} ms esiginin uzerinde.");

        CrawlViolation Vital(string code, Severity severity, int weight, string message) =>
            new(site.HomePageId,
                new RuleViolation(code, RuleCategory.Performance, severity, weight, message));
    }

    private static IReadOnlyList<string> Samples(IEnumerable<CrawlPageInput> pages) =>
        [.. pages.Select(p => p.Url).Take(MaxSampleUrls)];

    private static string TrimSlash(string url) =>
        url.Length > 1 ? url.TrimEnd('/') : url;

    private static string Clip(string value) =>
        value.Length <= 80 ? value : value[..80] + "…";
}
