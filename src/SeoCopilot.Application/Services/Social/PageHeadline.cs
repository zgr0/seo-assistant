using SeoCopilot.Domain.Entities.Crawling;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Sayfadan yayina uygun bir baslik secer. "Hakkimizda", "Anasayfa", "Iletisim" gibi
/// gezinme etiketleri bir baslik degildir — gonderiye ya da gorsele yazildiklarinda
/// okuyucuya hicbir sey soylemezler; bunlar elenip sayfanin kendi anlatimina inilir.
/// </summary>
public static class PageHeadline
{
    /// <summary>
    /// Tek basina anlam tasimayan gezinme etiketleri. Karsilastirma ASCII'ye indirgenmis
    /// bicimde yapilir, bu yuzden liste de ASCII yazilir ("iletisim" hem "İletişim" hem
    /// "iletisim" yazimini yakalar).
    /// </summary>
    private static readonly HashSet<string> Navigational = new(StringComparer.Ordinal)
    {
        "hakkimizda", "hakkinda", "biz kimiz", "kurumsal", "sirketimiz",
        "anasayfa", "ana sayfa", "baslangic", "giris",
        "iletisim", "bize ulasin", "iletisim bilgileri",
        "urunler", "urun", "hizmetler", "hizmetlerimiz", "cozumler", "cozumlerimiz",
        "blog", "haberler", "duyurular", "basin", "basinda biz", "makaleler", "yazilar",
        "galeri", "foto galeri", "video galeri", "referanslar", "musterilerimiz",
        "sss", "s.s.s", "sikca sorulan sorular", "yardim", "destek",
        "misyon", "vizyon", "misyon ve vizyon", "degerlerimiz", "tarihce",
        "kariyer", "insan kaynaklari", "bayilik", "bayiler", "katalog", "kataloglar",
        "sertifikalar", "belgelerimiz", "kalite", "politikalarimiz",
        "home", "about", "about us", "contact", "contact us", "products", "services",
        "news", "gallery", "references", "faq", "support", "careers"
    };

    /// <summary>Turkce harflerin ASCII karsiligi — ordinal karsilastirma 'İ' ile 'i'yi esitlemez.</summary>
    private static readonly Dictionary<char, char> AsciiMap = new()
    {
        ['ç'] = 'c', ['ğ'] = 'g', ['ı'] = 'i', ['ö'] = 'o', ['ş'] = 's', ['ü'] = 'u',
        ['â'] = 'a', ['î'] = 'i', ['û'] = 'u'
    };

    /// <summary>Gorsele ya da gonderiye yazilmaya deger bir baslik; yoksa null.</summary>
    public static string? Meaningful(Page page)
    {
        foreach (var candidate in Candidates(page))
        {
            var tidy = Tidy(candidate);
            if (tidy.Length > 0 && !IsNavigational(tidy)) return tidy;
        }

        return null;
    }

    /// <summary>Metnin tamami bir gezinme etiketinden ibaret mi.</summary>
    public static bool IsNavigational(string text) =>
        Navigational.Contains(Normalize(Tidy(text).TrimEnd('!', '?', '.')));

    /// <summary>Turkce kucuk harfe cevirip diakritikleri ASCII'ye indirger.</summary>
    private static string Normalize(string value)
    {
        var lowered = value.ToLower(Turkish);
        var sb = new System.Text.StringBuilder(lowered.Length);

        foreach (var ch in lowered)
            sb.Append(AsciiMap.TryGetValue(ch, out var ascii) ? ascii : ch);

        return sb.ToString();
    }

    private static readonly System.Globalization.CultureInfo Turkish = new("tr-TR");

    /// <summary>Once sayfanin kendi basliklari, sonra anlatim metinleri.</summary>
    private static IEnumerable<string> Candidates(Page page)
    {
        foreach (var h1 in page.H1Texts.Where(h => !string.IsNullOrWhiteSpace(h)))
            yield return h1;

        if (page.Title is { Length: > 0 } title)
        {
            // "Baslik | Marka | Kategori" — ilk parca en somut olani.
            yield return title.Split('|', '—', '–')[0];
            // Ilk parca gezinme etiketiyse ikinci parca markadir; basligi metinde ariyoruz.
        }

        // Gezinme etiketli sayfalarda anlam govdededir: aciklamanin ilk cumlesi.
        if (FirstSentence(page.MetaDescription) is { } meta) yield return meta;
        if (FirstSentence(page.MainText) is { } main) yield return main;
    }

    private static string? FirstSentence(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var sentence = text
            .Split(['.', '!', '?', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .FirstOrDefault(s => s.Length >= 25);

        return sentence;
    }

    private static string Tidy(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.', ',', ';', ':', ' ', '…');
}
