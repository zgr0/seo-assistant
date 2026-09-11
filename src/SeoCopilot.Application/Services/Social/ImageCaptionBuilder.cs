using SeoCopilot.Application.Abstractions;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Entities.Crawling;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Gorsele basilacak metni secer. Gonderi govdesi degil — gorselde ancak birkac kelime
/// okunur. Govde, CTA ve hashtag gonderi metninde kalir.
/// </summary>
public static class ImageCaptionBuilder
{
    /// <summary>Baslik bu uzunlugu gecerse kelime sinirinda kirpilir.</summary>
    public const int MaxHeadlineChars = 60;

    public const int MaxBrandLineChars = 40;

    public static ImageCaption? Build(ContentVariant variant, Page? page, BrandProfile? brand)
    {
        var headline = Headline(variant, page);
        if (headline is null) return null;

        return new ImageCaption(headline, BrandLine(page, brand));
    }

    /// <summary>
    /// Sayfanin anlamli basligi; "Hakkimizda" gibi gezinme etiketleri gorsele yazilmaz
    /// (bkz. <see cref="PageHeadline"/>). Hicbir aday kalmazsa null — gorsel yazisiz kalir.
    /// </summary>
    private static string? Headline(ContentVariant variant, Page? page)
    {
        if (page is not null && PageHeadline.Meaningful(page) is { } fromPage)
            return Clip(fromPage, MaxHeadlineChars);

        // Govdenin ilk satiri zaten kancadir — o da etiketse yaziyi hic basma.
        var firstLine = variant.Body
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(Tidy)
            .FirstOrDefault(l => l.Length > 0);

        return firstLine is null || PageHeadline.IsNavigational(firstLine)
            ? null
            : Clip(firstLine, MaxHeadlineChars);
    }

    /// <summary>Marka adi; yoksa sayfanin alan adi (www atilir).</summary>
    private static string? BrandLine(Page? page, BrandProfile? brand)
    {
        if (brand?.Name is { Length: > 0 } name) return Clip(Tidy(name), MaxBrandLineChars);

        if (page is not null && Uri.TryCreate(page.Url, UriKind.Absolute, out var uri))
        {
            var host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? uri.Host[4..]
                : uri.Host;
            return host.Length > 0 ? Clip(host, MaxBrandLineChars) : null;
        }

        return null;
    }

    private static string Tidy(string value)
    {
        var single = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        // Baslik sonundaki noktalama gorselde gereksiz.
        return single.TrimEnd('.', ',', ';', ':', ' ', '…');
    }

    /// <summary>Kelime ortasindan kesmez.</summary>
    private static string Clip(string value, int max)
    {
        if (value.Length <= max) return value;

        var cut = value[..max];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > max / 2) cut = cut[..lastSpace];

        return cut.TrimEnd(' ', ',', '.', ';', ':') + "…";
    }
}
