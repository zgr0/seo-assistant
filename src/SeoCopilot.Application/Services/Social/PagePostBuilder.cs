using System.Globalization;
using System.Text;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Tarama verisinden sablon tabanli gonderi uretir — LLM cagrisi yok. Model kapaliyken ya da
/// cagri basarisizken devreye girer. Yalniz sayfanin kendi metnini kullanir; iddia uydurmaz.
/// </summary>
public static class PagePostBuilder
{
    /// <summary>Platform siniri bilinmiyorsa varsayilan govde siniri.</summary>
    public const int DefaultMaxChars = 1000;

    /// <summary>Gonderi govdesine tasinan azami aciklama uzunlugu.</summary>
    private const int MaxValueChars = 400;

    private const int MaxHashtags = 8;

    /// <summary>Hashtag'e cevrilmeyecek kadar kisa kelimeler.</summary>
    private const int MinHashtagLength = 4;

    /// <summary>Varyant sirasina gore donen aci — ayni siteden gelen gonderiler benzemesin.</summary>
    private static readonly string[] Angles = ["bilgilendirici", "merak_uyandiran", "satis_odakli"];

    /// <summary>Turkce harfleri hashtag icin ASCII karsiligina cevirir.</summary>
    private static readonly Dictionary<char, char> AsciiMap = new()
    {
        ['ç'] = 'c', ['ğ'] = 'g', ['ı'] = 'i', ['ö'] = 'o', ['ş'] = 's', ['ü'] = 'u', ['â'] = 'a'
    };

    /// <param name="index">Varyant sirasi — aci ve sablon secimini belirler.</param>
    public static ContentVariant Build(
        Page page, PlatformProfile? platform, BrandProfile? brand, int index)
    {
        var angle = Angles[index % Angles.Length];
        var heading = Heading(page);
        var value = Value(page);
        var cta = Cta(page, platform, brand, angle);

        // Baslik gezinme etiketi oldugu icin aciklamadan turediyse govde onu tekrarlamasin.
        if (value is not null && (value.StartsWith(heading, StringComparison.OrdinalIgnoreCase)
            || heading.StartsWith(value, StringComparison.OrdinalIgnoreCase)))
        {
            value = null;
        }

        var body = Body(angle, heading, value, cta, platform);
        var hashtags = Hashtags(page, brand, platform);

        return new ContentVariant
        {
            VariantIndex = index,
            Angle = angle,
            Body = body,
            Hashtags = hashtags,
            Cta = cta,
            CharCount = body.Length,
            Description = value ?? heading,
            ImageBrief = PageBriefBuilder.Build(page),
            ImageAlt = PageBriefBuilder.BuildAlt(page)
        };
    }

    private static string Body(
        string angle, string heading, string? value, string cta, PlatformProfile? platform)
    {
        var limit = platform?.MaxChars ?? DefaultMaxChars;

        var sb = new StringBuilder();
        sb.AppendLine(Hook(angle, heading));

        if (value is { Length: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine(value);
        }

        sb.AppendLine();
        sb.Append(cta);

        var body = sb.ToString().Trim();
        return body.Length <= limit ? body : Clip(body, limit);
    }

    /// <summary>Kancaya soru eki ancak kisa bir baslikta yakisir.</summary>
    private const int MaxQuestionHookChars = 45;

    /// <summary>Ilk satir kanca — aciya gore degisir, iddia eklemez.</summary>
    private static string Hook(string angle, string heading) =>
        angle == "merak_uyandiran" && heading.Length <= MaxQuestionHookChars
            ? $"{heading} — nedir, ne işe yarar?"
            : heading;

    /// <summary>
    /// Gonderi kancasi. "Hakkimizda" gibi gezinme etiketleri kanca olmaz — sayfanin
    /// kendi anlatimina inilir (bkz. <see cref="PageHeadline"/>).
    /// </summary>
    private static string Heading(Page page) =>
        PageHeadline.Meaningful(page) is { } headline ? Clip(headline, MaxValueChars) : Tidy(page.Url);

    /// <summary>Govdenin degeri: meta description, yoksa ana metnin ilk cumleleri.</summary>
    private static string? Value(Page page)
    {
        if (page.MetaDescription is { Length: > 0 } meta) return Clip(Tidy(meta), MaxValueChars);

        if (page.MainText is not { Length: > 0 } text) return null;

        var sentences = text
            .Split(['.', '!', '?', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 30)
            .Take(2)
            .ToList();

        return sentences.Count == 0 ? null : Clip(string.Join(". ", sentences) + ".", MaxValueChars);
    }

    private static string Cta(Page page, PlatformProfile? platform, BrandProfile? brand, string angle)
    {
        var formal = brand?.AddressForm != AddressForm.Sen;

        var verb = angle switch
        {
            "satis_odakli" => formal ? "Teklif alın" : "Teklif al",
            "merak_uyandiran" => formal ? "Detayları inceleyin" : "Detayları incele",
            _ => formal ? "Ayrıntılar için sayfamıza göz atın" : "Ayrıntılar için sayfamıza göz at"
        };

        // Link paylasimi desteklenmiyorsa (Instagram) URL govdeye konmaz.
        return platform?.SupportsLinks == true ? $"{verb}: {page.Url}" : $"{verb} — bağlantı profilimizde.";
    }

    /// <summary>Marka varsayilanlari once; kalan kontenjan sayfa anahtar kelimeleriyle dolar.</summary>
    private static List<string> Hashtags(Page page, BrandProfile? brand, PlatformProfile? platform)
    {
        var limit = Math.Min(platform?.MaxHashtags ?? MaxHashtags, MaxHashtags);
        if (limit <= 0) return [];

        var tags = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in brand?.DefaultHashtags ?? [])
        {
            var normalized = Tag(tag);
            if (normalized is not null && seen.Add(normalized)) tags.Add(normalized);
            if (tags.Count == limit) return tags;
        }

        foreach (var word in Words(page))
        {
            var normalized = Tag(word);
            if (normalized is not null && seen.Add(normalized)) tags.Add(normalized);
            if (tags.Count == limit) break;
        }

        return tags;
    }

    /// <summary>Basliktaki ve ana metindeki anlamli kelimeler — basliktakiler once.</summary>
    private static IEnumerable<string> Words(Page page)
    {
        var heading = Heading(page);
        var text = page.MainText is { Length: > 0 } main
            ? main[..Math.Min(main.Length, 1500)]
            : string.Empty;

        return Split(heading).Concat(
            Split(text)
                .GroupBy(w => w, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => g.Key));

        static IEnumerable<string> Split(string value) => value
            .Split([' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '(', ')', '"', '\'', '-', '/'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= MinHashtagLength
                && w.All(char.IsLetter)
                && !TurkishStopWords.Contains(w));
    }

    /// <summary>Kelimeyi '#kelime' bicimine cevirir; cok kisa ya da harfsizse null.</summary>
    private static string? Tag(string word)
    {
        var trimmed = word.Trim().TrimStart('#');
        if (trimmed.Length < MinHashtagLength) return null;

        var sb = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed.ToLower(new CultureInfo("tr-TR")))
        {
            if (AsciiMap.TryGetValue(ch, out var ascii)) sb.Append(ascii);
            else if (char.IsAsciiLetterOrDigit(ch)) sb.Append(ch);
        }

        return sb.Length >= MinHashtagLength ? $"#{sb}" : null;
    }

    /// <summary>Fazla bosluklari ve satir sonlarini tek bosluga indirir.</summary>
    private static string Tidy(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Kelime ortasindan kesmez; sona uc nokta koyar.</summary>
    private static string Clip(string value, int max)
    {
        if (value.Length <= max) return value;

        var cut = value[..(max - 1)];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > max / 2) cut = cut[..lastSpace];

        return cut.TrimEnd(' ', ',', '.', ';', ':') + "…";
    }
}
