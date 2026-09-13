using System.Text.Json;
using System.Text.RegularExpressions;
using SeoCopilot.Domain.Entities.Crawling;

namespace SeoCopilot.Application.Services.Social;

/// <summary>Sosyal gonderi acisindan sayfa turu.</summary>
public enum PageKind
{
    /// <summary>Tekil haber/blog yazisi — gonderi icin en zengin kaynak.</summary>
    Article,

    /// <summary>Yazi listesi (/blog, /haberler) — baglanti yigini, kendi icerigi zayif.</summary>
    Listing,

    Other
}

/// <summary>
/// Sayfanin haber/blog yazisi olup olmadigini tarama verisinden cikarir. Sinyaller guclu
/// olandan zayifa: yapisal veri turu, og:type, URL yolu.
/// </summary>
public static partial class PageClassifier
{
    /// <summary>Yazi bolumlerinin URL segmentleri (ASCII, kucuk harf).</summary>
    private static readonly HashSet<string> ArticleSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "blog", "bloglar", "haber", "haberler", "news", "makale", "makaleler", "article", "articles",
        "yazi", "yazilar", "duyuru", "duyurular", "basin", "basinda-biz", "basin-bultenleri",
        "etkinlik", "etkinlikler", "events", "post", "posts", "insights", "gundem", "bulten"
    };

    /// <summary>schema.org yazi turleri.</summary>
    private static readonly HashSet<string> ArticleSchemaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Article", "NewsArticle", "BlogPosting", "TechArticle", "Report", "ScholarlyArticle"
    };

    /// <summary>Sayfalama segmentleri — liste sayfasinin ikinci sayfasi yazi degildir.</summary>
    private static readonly HashSet<string> PaginationSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "sayfa", "page", "p"
    };

    /// <param name="trustMarkup">
    /// Yapisal veri ve og:type'a guvenilsin mi. Bircok CMS her sayfaya — ana sayfa dahil —
    /// <c>og:type=article</c> basar; site genelinde yaygin olan isaret ayirt edici degildir.
    /// Karari site butununu goren <see cref="PageSelector"/> verir.
    /// </param>
    public static PageKind Classify(Page page, bool trustMarkup = true)
    {
        if (trustMarkup && HasArticleMarkup(page)) return PageKind.Article;

        var segments = Segments(page.Url);
        if (segments.Count == 0) return PageKind.Other;

        // /2024/05/baslik gibi tarihli yollar neredeyse her zaman yazidir.
        if (DatedPath().IsMatch(string.Join('/', segments))) return PageKind.Article;

        var sectionIndex = segments.FindIndex(IsSection);
        if (sectionIndex < 0) return PageKind.Other;

        // Bolumden sonra sayfalama disinda bir segment varsa tekil yazi; yoksa liste.
        var rest = segments
            .Skip(sectionIndex + 1)
            .Where(s => !PaginationSegments.Contains(s) && !s.All(char.IsDigit))
            .ToList();

        return rest.Count > 0 ? PageKind.Article : PageKind.Listing;
    }

    /// <summary>
    /// Duz bolum adi ("blog", "haberler") ya da birlesik bicimi ("blogyazisi", "haber-detay").
    /// Birlesik bicim yalniz bilinen eklerle kabul edilir: "haberlesme-sistemleri" bir urun
    /// sayfasidir, onek eslesmesi onu yanlislikla yazi sayardi.
    /// </summary>
    private static bool IsSection(string segment) =>
        ArticleSections.Contains(segment) || CompoundSection().IsMatch(segment);

    /// <summary>URL yolunun bos olmayan segmentleri, kucuk harf.</summary>
    internal static List<string> Segments(string url)
    {
        var path = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;

        return [.. path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Uri.UnescapeDataString(s).ToLowerInvariant())];
    }

    /// <summary>Sayfa yapisal veride ya da og:type'ta kendini yazi olarak bildiriyor mu.</summary>
    public static bool HasArticleMarkup(Page page) =>
        page.SchemaTypes.Any(ArticleSchemaTypes.Contains) || IsOgArticle(page.OgData);

    private static bool IsOgArticle(string? ogJson)
    {
        if (ogJson is not { Length: > 0 }) return false;

        try
        {
            using var doc = JsonDocument.Parse(ogJson);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("og:type", out var type)
                && type.ValueKind == JsonValueKind.String
                && string.Equals(type.GetString(), "article", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// yyyy/aa[/gg]/baslik — sonda harf iceren bir slug sart; /2024/05 gibi arsiv listeleri eslesmez.
    /// </summary>
    [GeneratedRegex(@"(^|/)(19|20)\d{2}/(0?[1-9]|1[0-2])(/\d{1,2})?/[^/]*[a-z][^/]*")]
    private static partial Regex DatedPath();

    /// <summary>kok [+ler/lar] [+ayrac] + bilinen ek — ör. blogyazisi, blog-yazilari, haberdetay, news-detail.</summary>
    [GeneratedRegex(
        @"^(blog|haber|news|makale|duyuru|article|post)(ler|lar)?[-_]?" +
        @"(yazi|yazisi|yazilari|yazilar|detay|detayi|detail|details|icerik|icerigi|post|posts)$")]
    private static partial Regex CompoundSection();
}
