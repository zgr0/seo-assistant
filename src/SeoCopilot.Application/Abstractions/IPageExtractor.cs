namespace SeoCopilot.Application.Abstractions;

/// <summary>Bir URL'i getirip DOM'dan SEO ile ilgili alanlari cikarir. Crawler katmani implemente eder.</summary>
public interface IPageExtractor
{
    Task<ExtractedPage> ExtractAsync(Uri url, PageFetchOptions options, CancellationToken ct = default);
}

/// <summary>Tek bir getirmenin davranisi. <see cref="BaseUri"/> goreli linkleri cozmek icin.</summary>
public sealed record PageFetchOptions(Uri BaseUri, bool RenderJs = false);

/// <summary>Sayfadan cikarilmis tek bir link.</summary>
public sealed record ExtractedLink(Uri Url, string? AnchorText, bool IsNofollow, bool IsInternal);

/// <summary>
/// Crawler'in bir sayfadan cikardigi ham veri. Hem pages tablosunu hem kural motorunu besler.
/// Alan sayisi fazla oldugu icin positional degil init-only — bkz. Rules.Model.PageInput.
/// </summary>
public sealed record ExtractedPage
{
    public required string Url { get; init; }

    /// <summary>0 → sayfa hic getirilemedi (DNS/timeout/socket).</summary>
    public required int StatusCode { get; init; }

    public string? ContentType { get; init; }
    public string? RedirectTo { get; init; }
    public int ResponseTimeMs { get; init; }
    public int HtmlSizeBytes { get; init; }

    public string? Title { get; init; }
    public string? MetaDescription { get; init; }
    public IReadOnlyList<string> H1 { get; init; } = [];
    public int H2Count { get; init; }
    public int WordCount { get; init; }

    public string? CanonicalUrl { get; init; }

    /// <summary>meta[name=robots] + X-Robots-Tag birlesimi.</summary>
    public string? RobotsMeta { get; init; }

    /// <summary>Open Graph alanlari, pages.og_data (jsonb) icin serilestirilmis.</summary>
    public string? OgDataJson { get; init; }

    public IReadOnlyList<string> SchemaTypes { get; init; } = [];

    public int ImagesTotal { get; init; }
    public int ImagesNoAlt { get; init; }

    public string? MainText { get; init; }
    public byte[]? ContentHash { get; init; }
    public string? Lang { get; init; }

    public IReadOnlyList<ExtractedLink> Links { get; init; } = [];

    /// <summary>Kural motoru uyumu — ic link URL'leri.</summary>
    public IReadOnlyList<string> InternalLinks =>
        [.. Links.Where(l => l.IsInternal).Select(l => l.Url.AbsoluteUri)];

    public bool HasCanonical => !string.IsNullOrWhiteSpace(CanonicalUrl);

    /// <summary>2xx donen ve HTML olarak ayristirilabilen sayfa mi.</summary>
    public bool IsHtml => StatusCode is >= 200 and < 300
        && ContentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true;

    public static ExtractedPage Failed(Uri url, int responseTimeMs) =>
        new() { Url = url.AbsoluteUri, StatusCode = 0, ResponseTimeMs = responseTimeMs };
}
