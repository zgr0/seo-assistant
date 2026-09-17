using System.Globalization;
using System.Net;
using System.Text;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services.Reporting;

/// <summary>
/// Tarama sonucundan kendi kendine yeten (harici varlik icermeyen) HTML rapor uretir.
///
/// Ozet rapordur: butun bulgular degil, genel skoru en cok asagi ceken birkac kural
/// anlatilir. Bulgularin tamami zaten panoda filtrelenebiliyor; rapora dokulunce PDF
/// onlarca sayfa oluyor ve okunmuyordu. Hedef iki sayfa.
/// </summary>
public static class HtmlReportRenderer
{
    /// <summary>Raporda ayrintisiyla anlatilan kural sayisi.</summary>
    public const int MaxPriorities = 5;

    /// <summary>
    /// Her kuralin altinda gosterilen ornek adres sayisi. Amac kuralin nerede tetiklendigini
    /// somutlastirmak, listeyi doldurmak degil — etkilenen sayfalarin tamami panoda.
    /// </summary>
    public const int MaxExampleUrls = 2;

    /// <summary>
    /// Oncelik maddesinde gosterilen duzeltme adimi sayisi. Bir tane: rapor "nereden
    /// baslamali"yi soyler, adimlarin tamami kural detayindadir. Ikiye cikarilinca her
    /// madde uc dort satir buyuyor ve rapor ikinci sayfaya tasiyor.
    /// </summary>
    private const int StepsShown = 1;

    /// <summary>Rapor Turkce; ondalik ayirici calisma ortaminin diline gore degismesin.</summary>
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static byte[] Render(
        Site site, Crawl crawl, IReadOnlyList<Issue> issues, Crawl? previous, DateOnly from, DateOnly to)
    {
        // Yoksayilan bulgu "yapilacak is" degil; onceliklerin disinda tutulur.
        var open = issues.Where(i => i.Status == IssueStatus.Open).ToList();

        var groups = open
            .GroupBy(i => i.RuleCode, StringComparer.Ordinal)
            .Select(g => new RuleGroup(g.Key, [.. g], Impact([.. g], crawl)))
            .OrderByDescending(g => g.Impact)
            .ThenByDescending(g => g.Issues.Max(i => (int)i.Severity))
            .ThenByDescending(g => g.Issues.Count)
            .ToList();

        var priorities = groups.Take(MaxPriorities).ToList();
        var topImpact = priorities.Count > 0 ? priorities[0].Impact : 0m;

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\">");
        sb.AppendLine($"<title>{H(site.Name)} — SEO raporu</title>");
        sb.AppendLine(Style);

        sb.AppendLine($"<h1>{H(site.Name)}</h1>");

        // Uretim damgasi ustte: sayfa sonunda ayri bir paragraf olarak dururken, icerik
        // sayfa sinirina yakin bittiginde tek basina bos bir sayfa daha aciyordu.
        sb.AppendLine($"<div class=\"sub\">{H(site.BaseUrl)} · {from:yyyy-MM-dd} – {to:yyyy-MM-dd}"
            + $" · tarama {crawl.Id} · üretim {DateTimeOffset.UtcNow:yyyy-MM-dd}</div>");

        // Sira: skor → skorun nerede dustugu → ne yapilacagi. Karne onceliklerden once
        // geldigi icin liste uzunsa tasma onceliklerin sonundan olur, karne ortadan bolunmez.
        sb.AppendLine(Hero(crawl, previous, open, issues.Count - open.Count));
        sb.AppendLine(CategoryScorecard(crawl));

        sb.AppendLine("<h2>Öncelikli işler</h2>");
        if (priorities.Count == 0)
        {
            sb.AppendLine("<p class=\"rest\">Açık bulgu yok — bu taramada düzeltilecek bir şey çıkmadı.</p>");
        }
        else
        {
            // Sira siddete gore degil skora etkiye gore: az sayida kritik bulgu, cok sayfaya
            // yayilmis orta bulgudan daha az puan goturebiliyor. Okuyan sasirmasin.
            sb.AppendLine("<p class=\"legend\">Genel skora etkisine göre sıralı — "
                + "çubuk maddeler arasındaki büyüklük farkını gösterir.</p>");
            sb.AppendLine("<ol class=\"priorities\">");
            foreach (var group in priorities) sb.AppendLine(Priority(group, topImpact));
            sb.AppendLine("</ol>");
            sb.AppendLine(Rest(groups, open));
        }

        sb.AppendLine("</body></html>");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private sealed record RuleGroup(string Code, IReadOnlyList<Issue> Issues, decimal Impact);

    // --- bolumler ---

    private static string Hero(Crawl crawl, Crawl? previous, IReadOnlyList<Issue> open, int ignored)
    {
        var facts = new List<string>
        {
            $"{crawl.PagesCrawled} sayfa tarandı",
            $"{open.Count} açık bulgu"
        };

        foreach (var severity in (Severity[])[Severity.Critical, Severity.High])
        {
            var count = open.Count(i => i.Severity == severity);
            if (count > 0) facts.Add($"{count} {SeverityText(severity)}");
        }

        if (ignored > 0) facts.Add($"{ignored} yoksayıldı");

        return $"""
            <div class="hero">
              <div class="score">{H(Number(crawl.OverallScore))}{Delta(crawl, previous)}</div>
              <div class="facts">{H(string.Join(" · ", facts))}</div>
            </div>
            """;
    }

    /// <summary>Kiyas taramasi verilmediyse bos doner.</summary>
    private static string Delta(Crawl crawl, Crawl? previous)
    {
        if (previous?.OverallScore is not decimal before || crawl.OverallScore is not decimal now)
            return string.Empty;

        var delta = now - before;
        var (tone, arrow) = delta switch
        {
            > 0 => ("up", "▲"),
            < 0 => ("down", "▼"),
            _ => ("flat", "=")
        };

        return $"<span class=\"delta {tone}\">{arrow} {H(Math.Abs(delta).ToString("0.0", Tr))}</span>";
    }

    private static string Priority(RuleGroup group, decimal topImpact)
    {
        var first = group.Issues[0];
        var severity = group.Issues.Max(i => i.Severity);
        var pageCount = group.Issues.Count(i => i.PageId is not null);

        // Cubuk mutlak bir puan vaadi degil: maddeler arasi buyukluk farkini gosterir.
        var width = topImpact <= 0 ? 0 : (int)Math.Round(group.Impact / topImpact * 100);

        var scope = pageCount > 0 ? $"{pageCount} sayfa" : "site geneli";

        return $"""
            <li>
              <div class="p-title">{H(first.Rule?.TitleTr ?? group.Code)}
                <span class="p-code">{H(group.Code)}</span></div>
              <div class="p-meta">
                <span class="{SeverityClass(severity)}">{H(SeverityText(severity))}</span>
                <span>{H(scope)}</span>
                <span class="bar" title="skora etkisi"><i style="width:{width}%"></i></span>
              </div>
              <div class="fix">{H(FirstSteps(first.Rule?.HowToFixTr))}{DocLink(first.Rule?.DocUrl)}</div>
              {Examples(group.Issues)}
            </li>
            """;
    }

    /// <summary>Kuralin somut olarak nerede tetiklendigi — birkac ornek adres.</summary>
    private static string Examples(IReadOnlyList<Issue> group)
    {
        var urls = group
            .Where(i => i.Page?.Url is not null)
            .Select(i => i.Page!.Url)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxExampleUrls)
            .ToList();

        if (urls.Count == 0) return string.Empty;

        var items = string.Concat(urls.Select(url => $"<li>{H(url)}</li>"));
        return $"<ul class=\"urls\">{items}</ul>";
    }

    /// <summary>Listeye girmeyen kurallarin tek satirlik ozeti.</summary>
    private static string Rest(IReadOnlyList<RuleGroup> groups, IReadOnlyList<Issue> open)
    {
        var restGroups = groups.Count - Math.Min(groups.Count, MaxPriorities);

        var note = restGroups == 0
            ? "Açık bulguların tamamı yukarıda."
            : $"Ayrıca {restGroups} kural daha, toplam "
              + $"{open.Count - groups.Take(MaxPriorities).Sum(g => g.Issues.Count)} bulgu — "
              + "etkisi düşük olduğu için listelenmedi.";

        return $"<p class=\"rest\">{note} Etkilenen sayfaların tam listesi panodadır.</p>";
    }

    /// <summary>
    /// Kategori karnesi. En zayif kategori basta; agirlik da yazilir cunku 100 uzerinden
    /// ayni eksik, dizinlenebilirlikte meta'dan cok daha pahaliya gelir.
    ///
    /// Tablo degil iki sutunlu izgara: sekiz satirlik tablo tek basina yarim sayfa yiyor ve
    /// ozet raporu ikinci sayfaya tasiyordu.
    /// </summary>
    private static string CategoryScorecard(Crawl crawl)
    {
        if (crawl.CategoryScores.Count == 0) return string.Empty;

        var cells = new StringBuilder();
        foreach (var (category, score) in crawl.CategoryScores.OrderBy(c => c.Value))
        {
            var weight = crawl.ScoringSnapshot.GetValueOrDefault($"category_weight_{category}");
            cells.AppendLine($"""
                <div class="cat">
                  <div class="cat-name">{H(CategoryText(category))}</div>
                  <div><span class="cat-score">{H(score.ToString("0.0", Tr))}</span>
                    {(weight > 0 ? $"<span class=\"cat-weight\">{H($"ağırlık %{weight}")}</span>" : string.Empty)}</div>
                </div>
                """);
        }

        return $"<h2>Kategori karnesi</h2><div class=\"cats\">{cells}</div>";
    }

    // --- skor etkisi ---

    /// <summary>
    /// Kural grubunun genel skoru asagi cekme payi: kategori icindeki cezasi x kategori agirligi.
    /// Ceza puanlari ve agirliklar crawl'in kendi <c>scoring_snapshot</c>'indan okunur, boylece
    /// eski raporlar o gunun agirliklariyla yorumlanir. Snapshot bos ise 0 doner ve siralama
    /// siddet + adede duser.
    ///
    /// Sayfa bulgusunun cezasi sayfa sayisina bolunur, site geneli bulgu bolunmez —
    /// skorlamadaki kural ile ayni (bkz. Rules/ScoreCalculator).
    /// </summary>
    private static decimal Impact(IReadOnlyList<Issue> group, Crawl crawl)
    {
        var pages = Math.Max(crawl.PagesCrawled, 1);

        var pagePenalty = group
            .Where(i => i.PageId is not null)
            .Sum(i => (decimal)Penalty(crawl, i.Severity)) / pages;

        var siteWide = group.Where(i => i.PageId is null).ToList();
        var sitePenalty = siteWide.Count == 0 ? 0m : Penalty(crawl, siteWide.Max(i => i.Severity));

        return (pagePenalty + sitePenalty) * Weight(crawl, group[0].Rule?.Category);
    }

    private static int Penalty(Crawl crawl, Severity severity) =>
        crawl.ScoringSnapshot.GetValueOrDefault(severity.ToString().ToLowerInvariant());

    private static int Weight(Crawl crawl, RuleCategory? category) =>
        category is null
            ? 0
            : crawl.ScoringSnapshot.GetValueOrDefault($"category_weight_{SnakeCase(category.Value.ToString())}");

    /// <summary>'StructuredData' -> 'structured_data' — scoring_snapshot anahtarlarinin bicimi.</summary>
    private static string SnakeCase(string name)
    {
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i])) sb.Append('_');
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }

    // --- metin ---

    /// <summary>
    /// Kural katalogundaki duzeltme adimlarinin ilk birkaci. Tam metin kural detay
    /// sayfasindadir; rapora kural basina ~1300 karakter sigmaz.
    /// </summary>
    private static string FirstSteps(string? howToFix)
    {
        if (string.IsNullOrWhiteSpace(howToFix)) return string.Empty;

        var steps = howToFix.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var shown = string.Join('\n', steps.Take(StepsShown));

        return steps.Length > StepsShown
            ? shown + $"\n… ({steps.Length - StepsShown} adım daha, kural detayında)"
            : shown;
    }

    /// <summary>Kuralin birincil kaynagi; seed'te tanimsizsa hicbir sey basilmaz.</summary>
    private static string DocLink(string? docUrl) => string.IsNullOrWhiteSpace(docUrl)
        ? string.Empty
        : $"<br><a class=\"doc\" href=\"{H(docUrl)}\">Kaynak doküman</a>";

    private static string Number(decimal? score) => score?.ToString("0.0", Tr) ?? "—";

    private static string SeverityClass(Severity severity) => severity.ToString().ToLowerInvariant();

    private static string SeverityText(Severity severity) => severity switch
    {
        Severity.Critical => "kritik",
        Severity.High => "yüksek",
        Severity.Medium => "orta",
        Severity.Low => "düşük",
        _ => "bilgi"
    };

    /// <summary>scoring_snapshot / category_scores anahtarlarini okunur baslığa cevirir.</summary>
    private static string CategoryText(string key) => key switch
    {
        "indexability" => "Dizinlenebilirlik",
        "meta" => "Meta etiketler",
        "content" => "İçerik",
        "links" => "Bağlantılar",
        "performance" => "Performans",
        "images" => "Görseller",
        "structured_data" => "Yapısal veri",
        "i18n" => "Dil ve bölge",
        _ => key
    };

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private const string Style =
        """
        <style>
          /* system-ui KULLANMA: konteynerde fontconfig'in varsayilan sans'i WenQuanYi Zen
             Hei'ye cozuluyor, onda da g/I/s yok — o uc harf baska yazi tipine dusup
             kelimenin ortasinda font degistiriyor. Arial, Liberation Sans'a eslenir. */
          body{font-family:Arial,Helvetica,sans-serif;margin:32px;color:#111}
          h1{margin:0 0 4px;font-size:23px}
          h2{margin:26px 0 10px;font-size:13px;text-transform:uppercase;
             letter-spacing:.07em;color:#666}
          .sub{color:#777;font-size:12px;margin-bottom:18px;overflow-wrap:anywhere}

          .hero{display:flex;align-items:baseline;gap:18px;flex-wrap:wrap;
                border:1px solid #e3e3e3;border-radius:10px;padding:14px 18px}
          .score{font-size:38px;font-weight:700;line-height:1}
          .delta{font-size:15px;font-weight:600;margin-left:8px}
          .up{color:#0a7a3d} .down{color:#b00020} .flat{color:#777}
          .facts{color:#444;font-size:13px}

          ol.priorities{margin:0;padding-left:20px}
          ol.priorities>li{margin:0 0 10px;padding-left:2px}
          .p-title{font-weight:600;font-size:13px}
          .p-code{font-weight:400;color:#999;font-size:11px}
          .p-meta{font-size:11px;color:#666;margin:3px 0 5px}
          .p-meta>span{margin-right:10px}
          .bar{display:inline-block;width:88px;height:6px;background:#eee;
               border-radius:3px;overflow:hidden;vertical-align:middle}
          .bar>i{display:block;height:100%;background:#555}
          .fix{white-space:pre-line;font-size:12px;color:#222}
          .doc{color:#0645ad;font-size:11px}
          ul.urls{margin:5px 0 0;padding-left:16px;font-size:11px;color:#0645ad}
          ul.urls>li{overflow-wrap:anywhere}
          .legend{font-size:11px;color:#888;margin:-4px 0 8px}
          .rest{font-size:12px;color:#666;margin:2px 0 0}

          /* Dort sutun, iki satir. Sekiz satirlik dikey liste ozet raporun yarim sayfasini
             yiyordu; skorlar da 80-100 arasinda sikisik oldugu icin cubuk bilgi vermiyordu. */
          .cats{display:grid;grid-template-columns:repeat(4,1fr);gap:10px 16px}
          .cat{border-top:2px solid #eee;padding-top:5px}
          .cat-name{font-size:10px;color:#777;margin-bottom:1px}
          .cat-score{font-size:15px;font-weight:700}
          .cat-weight{font-size:10px;color:#999;margin-left:5px}

          .critical{color:#b00020;font-weight:600} .high{color:#c25e00;font-weight:600}
          .medium{color:#8a6d00} .low{color:#555}

          @page{size:A4;margin:18mm 14mm}
          @media print{
            /* Kenar boslugu @page'den geliyor; govde marjini ustune binmesin. */
            body{margin:0}
            h2{break-after:avoid}
            ol.priorities>li{break-inside:avoid}
            .cat{break-inside:avoid}
          }
        </style></head><body>
        """;
}
