using System.Text;
using System.Text.Json;
using SeoCopilot.Domain.Entities.Crawling;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Tarama verisinden gorsel brief'i uretir — LLM cagrisi yok. Model istemi yazamadiginda
/// (anahtar yok, kota bitti, sema disi yanit) gorsel yine de uretilebilsin diye vardir.
/// Konu sayfanin kendi sinyallerinden gelir; stil ve negatifler sabittir.
/// </summary>
public static class PageBriefBuilder
{
    /// <summary>Konu metninde tasinan azami karakter — istem sismesin.</summary>
    public const int MaxSubjectChars = 180;

    /// <summary>Anahtar kelime cikarimi icin taranan azami ana metin.</summary>
    private const int MaxScanChars = 2000;

    private const int KeywordCount = 4;

    private const int MinKeywordLength = 4;

    /// <summary>Fotograf yonergesi — model her zaman ayni gorsel dili uretsin diye sabit.</summary>
    private const string StyleSuffix =
        "professional commercial photography, natural lighting, shallow depth of field, " +
        "clean composition, high detail, no text, no letters, no watermark, no logo";

    /// <summary>Konu tasiyamayan sayfalarda kullanilan notr sahne.</summary>
    private const string FallbackSubject = "a modern workspace representing an online business";

    /// <summary>Sayfa icin gorsel istemi; <paramref name="page"/> zayifsa notr sahneye duser.</summary>
    public static string Build(Page page)
    {
        var subject = Subject(page);
        var keywords = Keywords(page);

        var sb = new StringBuilder();
        sb.Append(subject);
        if (keywords.Count > 0) sb.Append(". Konu: ").Append(string.Join(", ", keywords));
        sb.Append(". ").Append(StyleSuffix);

        return sb.ToString();
    }

    /// <summary>Gorsel alternatif metni — erisilebilirlik icin sayfa basligina dayanir.</summary>
    public static string BuildAlt(Page page) =>
        Heading(page) is { Length: > 0 } heading
            ? $"{heading} konusunu temsil eden görsel"
            : "Gönderiye eşlik eden temsili görsel";

    private static string Subject(Page page)
    {
        var heading = Heading(page);
        var context = Context(page);

        if (heading is null && context is null) return FallbackSubject;
        if (heading is null) return Clip(context!, MaxSubjectChars);
        if (context is null) return Clip(heading, MaxSubjectChars);

        return Clip($"{Trim(heading)}. {Trim(context)}", MaxSubjectChars);
    }

    /// <summary>
    /// Sayfanin anlamli basligi; gezinme etiketleri ("Hakkimizda") sahne tarif etmez,
    /// o yuzden elenir ve og:title'a ya da metne inilir.
    /// </summary>
    private static string? Heading(Page page)
    {
        if (PageHeadline.Meaningful(page) is { } headline) return headline;

        var og = OpenGraph(page, "og:title");
        return og is not null && !PageHeadline.IsNavigational(og) ? og : null;
    }

    private static string? Context(Page page)
    {
        if (page.MetaDescription is { Length: > 0 } meta) return meta.Trim();
        if (OpenGraph(page, "og:description") is { Length: > 0 } og) return og;

        // Ana metnin ilk cumlesi — baslik disinda tek somut baglam.
        if (page.MainText is { Length: > 0 } text)
        {
            var sentence = text.Split(['.', '!', '?', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(s => s.Trim().Length > 20);
            if (sentence is not null) return sentence.Trim();
        }

        return null;
    }

    private static string? OpenGraph(Page page, string property)
    {
        if (page.OgData is not { Length: > 0 } json) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            return doc.RootElement.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() is { Length: > 0 } text
                ? text.Trim()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Ana metindeki en sik gecen anlamli kelimeler — sahneye konu verir.</summary>
    private static List<string> Keywords(Page page)
    {
        if (page.MainText is not { Length: > 0 } text) return [];

        var scan = text.Length > MaxScanChars ? text[..MaxScanChars] : text;

        return [.. scan
            .Split([' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '(', ')', '"', '\'', '-', '/'],
                StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Trim().ToLowerInvariant())
            .Where(word => word.Length >= MinKeywordLength && !TurkishStopWords.Contains(word))
            .Where(word => word.All(char.IsLetter))
            .GroupBy(word => word)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Take(KeywordCount)
            .Select(group => group.Key)];
    }

    private static string Clip(string value, int max) =>
        value.Length <= max ? value : value[..max].TrimEnd();

    /// <summary>Parcalar noktayla birlestigi icin sondaki noktalama yinelenmesin.</summary>
    private static string Trim(string value) => value.TrimEnd('.', '!', '?', ' ', '…');
}
