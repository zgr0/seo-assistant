using SeoCopilot.Domain.Entities.Crawling;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Taranan sayfalar arasindan sosyal medya gonderisi uretmeye deger olanlari secer.
/// Once icerigi doyurucu sayfalar, sonra ana sayfa; ardindan ic link sayisi ve metin uzunlugu.
/// </summary>
public static class PageSelector
{
    /// <summary>Bu esigin ustundeki sayfalar once gelir; alti da kullanilir (kucuk siteler icin).</summary>
    public const int RichTextChars = 200;

    /// <summary>
    /// Yasal/islevsel sayfalar: her sayfadan link aldiklari icin ic link siralamasinda tepeye
    /// cikarlar ama sosyal gonderi degeri yoktur. URL yolunda gecerse sayfa elenir.
    /// </summary>
    private static readonly string[] BoilerplateSegments =
    [
        "kvkk", "kisisel-veri", "gizlilik", "privacy", "cerez", "cookie",
        "kullanim-kosullari", "kullanim-sartlari", "terms", "mesafeli", "iade",
        "sozlesme", "aydinlatma", "sitemap", "site-haritasi",
        "sepet", "cart", "checkout", "odeme", "giris", "login", "uyelik", "hesabim",
        "arama", "search", "etiket", "tag", "yazar", "author", "sayfa/"
    ];

    public static IReadOnlyList<Page> Select(IEnumerable<Page> pages, int count) =>
        [.. pages
            .Where(IsUsable)
            .OrderByDescending(p => (p.MainText?.Length ?? 0) >= RichTextChars)
            .ThenBy(p => p.Depth)
            .ThenByDescending(p => p.InlinkCount)
            .ThenByDescending(p => p.WordCount)
            .Take(Math.Max(count, 1))];

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

    private static bool IsBoilerplate(string url)
    {
        var path = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
        return BoilerplateSegments.Any(segment =>
            path.Contains(segment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNoIndex(string? robotsMeta) =>
        robotsMeta is { Length: > 0 }
        && robotsMeta.Contains("noindex", StringComparison.OrdinalIgnoreCase);
}
