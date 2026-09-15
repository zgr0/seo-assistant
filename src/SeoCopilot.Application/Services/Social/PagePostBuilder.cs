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

    /// <summary>Haber/blog yazisinda satis acisi yakismaz — yazinin fikri aktarilir.</summary>
    private static readonly string[] ArticleAngles = ["bilgilendirici", "merak_uyandiran"];

    /// <summary>Turkce harfleri hashtag icin ASCII karsiligina cevirir.</summary>
    private static readonly Dictionary<char, char> AsciiMap = new()
    {
        ['ç'] = 'c', ['ğ'] = 'g', ['ı'] = 'i', ['ö'] = 'o', ['ş'] = 's', ['ü'] = 'u', ['â'] = 'a'
    };

    /// <param name="index">Varyant sirasi — aci ve sablon secimini belirler.</param>
    /// <param name="kind">
    /// Site butununu goren secicinin verdigi tur. Verilmezse sayfa tek basina siniflanir —
    /// her sayfaya og:type=article basan sitelerde bu yaniltabilir.
    /// </param>
    public static ContentVariant Build(
        Page page, PlatformProfile? platform, BrandProfile? brand, int index, PageKind? kind = null)
    {
        var article = (kind ?? PageClassifier.Classify(page)) == PageKind.Article;
        var angles = article ? ArticleAngles : Angles;
        index = Math.Max(index, 0);
        var angle = angles[index % angles.Length];

        // Acilar bitince tur artar: ayni sayfa yeniden islendiginde kanca, eylem cagrisi ve
        // alinan cumleler degisir. Ilk tur (round 0) klasik sablondur.
        var round = index / angles.Length;
        var formal = brand?.AddressForm != AddressForm.Sen;

        var heading = Heading(page);
        var value = Value(page, round);
        var cta = Cta(page, platform, formal, angle, article, round / HookCount);

        // Baslik gezinme etiketi oldugu icin aciklamadan turediyse govde onu tekrarlamasin.
        if (value is not null && (value.StartsWith(heading, StringComparison.OrdinalIgnoreCase)
            || heading.StartsWith(value, StringComparison.OrdinalIgnoreCase)))
        {
            value = null;
        }

        var body = Body(Hook(angle, heading, formal, round % HookCount), value, cta, platform);
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

    private static string Body(string hook, string? value, string cta, PlatformProfile? platform)
    {
        var limit = platform?.MaxChars ?? DefaultMaxChars;

        var sb = new StringBuilder();
        sb.AppendLine(hook);

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

    /// <summary>Tur basina kanca kalibi sayisi; eylem cagrisi bunun katlarinda doner.</summary>
    private const int HookCount = 3;

    /// <summary>
    /// Ilk satir kanca — aciya ve tura gore degisir, iddia eklemez. Kalip 0 klasik kancadir;
    /// digerleri basligi degistirmeden cerceveler.
    /// </summary>
    private static string Hook(string angle, string heading, bool formal, int variant)
    {
        var shortHeading = heading.Length <= MaxQuestionHookChars;

        return (angle, variant) switch
        {
            ("merak_uyandiran", 0) => shortHeading ? $"{heading} — nedir, ne işe yarar?" : heading,
            ("merak_uyandiran", 1) => shortHeading ? $"{heading}: işin aslı ne?" : $"Merak edenler için: {heading}",
            ("merak_uyandiran", _) => $"Hiç düşündün{(formal ? "üz" : "")} mü? {heading}",
            ("satis_odakli", 1) => $"{(formal ? "Aradığınız" : "Aradığın")} çözüm: {heading}",
            ("satis_odakli", 2) => $"{heading} için doğru adres",
            (_, 1) => $"Yakından bakalım: {heading}",
            (_, 2) => $"Kısaca {heading}",
            _ => heading
        };
    }

    /// <summary>
    /// Gonderi kancasi. "Hakkimizda" gibi gezinme etiketleri kanca olmaz — sayfanin
    /// kendi anlatimina inilir (bkz. <see cref="PageHeadline"/>).
    /// </summary>
    private static string Heading(Page page) =>
        PageHeadline.Meaningful(page) is { } headline ? Clip(headline, MaxValueChars) : Tidy(page.Url);

    /// <summary>Govdeye tek seferde alinan ana metin cumlesi.</summary>
    private const int SentencesPerValue = 2;

    /// <summary>
    /// Govdenin degeri. Ilk turda meta description (yoksa ana metnin ilk cumleleri); sonraki
    /// turlarda ana metnin siradaki cumleleri — ayni sayfadan ikinci gonderi baska bir seyi anlatir.
    /// </summary>
    private static string? Value(Page page, int round)
    {
        var meta = page.MetaDescription is { Length: > 0 } m ? Clip(Tidy(m), MaxValueChars) : null;
        if (round == 0 && meta is not null) return meta;

        var sentences = page.MainText is { Length: > 0 } text
            ? text
                .Split(['.', '!', '?', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Tidy(s))
                .Where(s => s.Length > 30)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            : [];

        if (sentences.Count == 0) return meta;

        // Meta varsa ilk tur onu kullandi; cumleler ikinci turdan itibaren bastan alinir.
        var chunk = meta is null ? round : round - 1;
        var chunks = (sentences.Count + SentencesPerValue - 1) / SentencesPerValue;

        // Cumleler bitince meta ile donusumlu basa sarilir.
        var slot = chunk % (chunks + (meta is null ? 0 : 1));
        if (slot == chunks) return meta;

        var picked = sentences.Skip(slot * SentencesPerValue).Take(SentencesPerValue);
        return Clip(string.Join(". ", picked) + ".", MaxValueChars);
    }

    private static string Cta(
        Page page, PlatformProfile? platform, bool formal, string angle, bool article, int variant)
    {
        var verbs = (article, angle) switch
        {
            // Yazida eylem okumaktir; teklif/inceleme cagrisi haberin tonuna uymaz.
            (true, "merak_uyandiran") => new[] { "Devamı yazıda", "Yazının devamı sayfamızda", "Ayrıntılar yazıda" },
            (true, _) => formal
                ? ["Yazının tamamını okuyun", "Tüm yazıyı okuyun", "Yazının tamamı sayfamızda"]
                : ["Yazının tamamını oku", "Tüm yazıyı oku", "Yazının tamamı sayfamızda"],
            (_, "satis_odakli") => formal
                ? ["Teklif alın", "Hemen bize ulaşın", "Bilgi ve teklif için iletişime geçin"]
                : ["Teklif al", "Hemen bize ulaş", "Bilgi ve teklif için iletişime geç"],
            (_, "merak_uyandiran") => formal
                ? ["Detayları inceleyin", "Cevabı sayfamızda bulun", "Merak ettikleriniz sayfamızda"]
                : ["Detayları incele", "Cevabı sayfamızda bul", "Merak ettiklerin sayfamızda"],
            _ => formal
                ? ["Ayrıntılar için sayfamıza göz atın", "Tüm detaylar sayfamızda", "Daha fazlası için sayfamızı ziyaret edin"]
                : ["Ayrıntılar için sayfamıza göz at", "Tüm detaylar sayfamızda", "Daha fazlası için sayfamızı ziyaret et"]
        };
        var verb = verbs[variant % verbs.Length];

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
