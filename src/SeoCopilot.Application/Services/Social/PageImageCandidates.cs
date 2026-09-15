using System.Text.Json;
using SeoCopilot.Domain.Entities.Crawling;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Sayfanin kendi gorsellerinden gonderi gorseli adaylarini secer. Logolar, simgeler ve
/// her sayfada tekrarlanan sablon gorselleri (ust bilgi logosu, kenar cubugu katalogu)
/// fotograf degildir — ad kaliplari ve site geneli siklikla elenir.
/// </summary>
public static class PageImageCandidates
{
    /// <summary>Tek sayfa icin denenecek azami aday — her biri bir indirme demek.</summary>
    public const int MaxCandidates = 5;

    /// <summary>Taranan sayfalarin bu oranindan fazlasinda gecen gorsel sablona aittir.</summary>
    public const double SiteWideRatio = 0.3;

    /// <summary>Oran hesabinin anlamli olmasi icin gereken en az sayfa.</summary>
    private const int MinPagesForRatio = 4;

    /// <summary>Fotograf olmayan gorsellerin adlarinda gecen kaliplar.</summary>
    private static readonly string[] NonPhotoTokens =
    [
        "logo", "icon", "favicon", "flag", "sprite", "avatar", "placeholder", "loading",
        "spinner", "pixel", "tracking", "badge", "payment", "social", "whatsapp", "arrow",
        "button", "blank", "spacer", "gravatar", "emoji", "captcha", "qr"
    ];

    /// <summary>Fotograf tasimayan dosya turleri.</summary>
    private static readonly string[] NonPhotoExtensions = [".svg", ".gif", ".ico", ".bmp"];

    /// <summary>
    /// Tum taranan sayfalarda gecen gorsellerin sayisi — site geneli sablon gorsellerini bulmak icin.
    /// </summary>
    public static IReadOnlySet<string> SiteWideImages(IReadOnlyCollection<Page> pages)
    {
        if (pages.Count < MinPagesForRatio) return new HashSet<string>();

        var threshold = pages.Count * SiteWideRatio;

        return pages
            .SelectMany(p => p.ImageUrls.Concat(OgImage(p) is { } og ? [og] : []).Distinct())
            .GroupBy(url => url, StringComparer.Ordinal)
            .Where(g => g.Count() > threshold)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Sayfanin adaylari, onem sirasiyla: og:image (sayfaya ozgu ise), sonra govdedeki
    /// gorseller belge sirasinda.
    /// </summary>
    public static IReadOnlyList<string> For(Page page, IReadOnlySet<string> siteWide) =>
        [.. (OgImage(page) is { } og ? new[] { og } : [])
            .Concat(page.ImageUrls)
            .Distinct(StringComparer.Ordinal)
            .Where(url => !siteWide.Contains(url) && LooksLikePhoto(url))
            .Take(MaxCandidates)];

    /// <summary>Adres kaliplarina gore fotograf olabilir mi (boyut indirmeden sonra olculur).</summary>
    public static bool LooksLikePhoto(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        var path = Uri.UnescapeDataString(uri.AbsolutePath).ToLowerInvariant();
        if (NonPhotoExtensions.Any(path.EndsWith)) return false;

        // Yalniz dosya adina ve son klasore bakilir: "/uploads/logos-archive/..." gibi
        // bir ust klasor ya da alan adi yuzunden gercek fotograf elenmesin.
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var tail = string.Join('/', segments.TakeLast(2));

        return !NonPhotoTokens.Any(token => tail.Contains(token, StringComparison.Ordinal));
    }

    private static string? OgImage(Page page)
    {
        if (page.OgData is not { Length: > 0 } json) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("og:image", out var value)
                && value.ValueKind == JsonValueKind.String
                && Uri.TryCreate(page.Url, UriKind.Absolute, out var pageUri)
                && Uri.TryCreate(pageUri, value.GetString(), out var absolute)
                    ? absolute.AbsoluteUri
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
