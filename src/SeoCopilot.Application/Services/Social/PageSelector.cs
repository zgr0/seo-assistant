using SeoCopilot.Domain.Entities.Crawling;

namespace SeoCopilot.Application.Services.Social;

public record SelectedPage(Page Page, PageKind Kind);

/// <summary>
/// Taranan sayfalar arasindan sosyal medya gonderisi uretmeye deger olanlari secer.
/// Haber ve blog yazilari once gelir — gonderiye en cok malzeme onlarda. Ardindan icerigi
/// doyurucu sayfalar; yazi listeleri (/blog, /haberler) en sona itilir.
/// </summary>
public static class PageSelector
{
    /// <summary>Bu esigin ustundeki sayfalar once gelir; alti da kullanilir (kucuk siteler icin).</summary>
    public const int RichTextChars = 200;

    /// <summary>
    /// Yasal/islevsel sayfalar: her sayfadan link aldiklari icin ic link siralamasinda tepeye
    /// cikarlar ama sosyal gonderi degeri yoktur.
    ///
    /// Kisa kelimeler yalniz <b>tam segment</b> olarak eslesir: alt dize ya da onek aramasi
    /// "/haber/karaman-fuari" (arama), "/blog/girisimcilik" (giris) ve
    /// "/urun/etiketleme-makinasi" (etiket) gibi gercek icerigi elerdi.
    /// </summary>
    private static readonly HashSet<string> BoilerplateExact = new(StringComparer.Ordinal)
    {
        "kvkk", "gizlilik", "privacy", "cerez", "cookie", "cookies", "terms", "iade", "sozlesme",
        "aydinlatma", "sitemap", "site-haritasi", "sepet", "cart", "checkout", "odeme",
        "giris", "login", "uyelik", "hesabim", "arama", "search",
        "etiket", "tag", "tags", "yazar", "author", "kategori", "category"
    };

    /// <summary>Uzun, belirsizligi olmayan yasal sayfa kaliplari — onek olarak eslesir.</summary>
    private static readonly string[] BoilerplatePrefixes =
    [
        "kvkk-", "kisisel-veri", "gizlilik-", "cerez-", "cookie-", "privacy-", "terms-",
        "kullanim-kosul", "kullanim-sart", "mesafeli-satis", "aydinlatma-metni",
        "iade-", "teslimat-", "uyelik-sozlesmesi"
    ];

    /// <summary>
    /// Sayfalarin bu oranindan fazlasi kendini yazi olarak isaretliyorsa isaret site geneli
    /// bir varsayilandir (ör. her sayfada og:type=article) ve yok sayilir.
    /// </summary>
    public const double MarkupTrustRatio = 0.5;

    /// <summary>Oran hesabinin anlamli olmasi icin gereken en az sayfa.</summary>
    private const int MinPagesForRatio = 4;

    public static IReadOnlyList<Page> Select(IEnumerable<Page> pages, int count) =>
        [.. SelectWithKinds(pages, count).Select(x => x.Page)];

    /// <summary>
    /// Secilen sayfalar ve site butunune gore verilmis tur karari. Tur, gonderi sablonuna ve
    /// model istemine iletilir — sayfa tek basina siniflanirsa site geneli og:type yaniltir.
    /// </summary>
    public static IReadOnlyList<SelectedPage> SelectWithKinds(IEnumerable<Page> pages, int count)
    {
        var usable = pages.Where(IsUsable).ToList();
        var trustMarkup = ShouldTrustMarkup(usable);

        return [.. usable
            .Select(page => new SelectedPage(page, PageClassifier.Classify(page, trustMarkup)))
            .OrderByDescending(x => x.Kind == PageKind.Article)
            .ThenByDescending(x => (x.Page.MainText?.Length ?? 0) >= RichTextChars)
            .ThenBy(x => x.Kind == PageKind.Listing)
            // Yazilarda derinlik anlamsiz (hepsi bolumun altinda); oteki sayfalarda ust seviye once.
            .ThenBy(x => x.Kind == PageKind.Article ? 0 : x.Page.Depth)
            // Yazilarda uzun olan daha doyurucu; oteki sayfalarda cok link alan daha onemli.
            .ThenByDescending(x => x.Kind == PageKind.Article ? x.Page.WordCount : x.Page.InlinkCount)
            .ThenByDescending(x => x.Page.WordCount)
            .Take(Math.Max(count, 1))];
    }

    private static bool ShouldTrustMarkup(IReadOnlyCollection<Page> pages)
    {
        if (pages.Count < MinPagesForRatio) return true;

        var marked = pages.Count(PageClassifier.HasArticleMarkup);
        return (double)marked / pages.Count <= MarkupTrustRatio;
    }

    /// <summary>
    /// Yayina uygun sayfa: 200 donen, yonlendirme olmayan, dizine acik ve modele verilecek
    /// en az bir metin tasiyan (ana metin ya da baslik) sayfa.
    /// </summary>
    private static bool IsUsable(Page page) =>
        page.StatusCode == 200
        && page.RedirectTo is null
        && (page.MainText is { Length: > 0 } || page.Title is { Length: > 0 })
        && !IsNoIndex(page.RobotsMeta)
        && !IsBoilerplate(page.Url);

    private static bool IsBoilerplate(string url) =>
        PageClassifier.Segments(url).Any(segment =>
            BoilerplateExact.Contains(segment)
            || BoilerplatePrefixes.Any(prefix => segment.StartsWith(prefix, StringComparison.Ordinal)));

    private static bool IsNoIndex(string? robotsMeta) =>
        robotsMeta is { Length: > 0 }
        && robotsMeta.Contains("noindex", StringComparison.OrdinalIgnoreCase);
}
