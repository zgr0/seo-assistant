using System.Net;
using System.Text;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Entities.Sites;

namespace SeoCopilot.Application.Services.Reporting;

/// <summary>
/// Tarama sonucundan kendi kendine yeten (harici varlik icermeyen) HTML rapor uretir.
/// Tarayicidan PDF'e basilabilir; obje deposunda tek dosya olarak saklanir.
/// </summary>
public static class HtmlReportRenderer
{
    /// <summary>Raporda listelenen azami bulgu sayisi.</summary>
    public const int MaxIssuesListed = 200;

    public static byte[] Render(
        Site site, Crawl crawl, IReadOnlyList<Issue> issues, Crawl? previous, DateOnly from, DateOnly to)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\">");
        sb.AppendLine($"<title>{H(site.Name)} — SEO raporu</title>");
        sb.AppendLine("""
            <style>
              body{font-family:system-ui,Segoe UI,Arial,sans-serif;margin:32px;color:#111;}
              h1{margin:0 0 4px;font-size:24px} h2{margin:28px 0 8px;font-size:18px}
              .sub{color:#666;margin-bottom:24px}
              .cards{display:flex;gap:12px;flex-wrap:wrap}
              .card{border:1px solid #e3e3e3;border-radius:8px;padding:12px 16px;min-width:140px}
              .card b{display:block;font-size:22px}
              table{border-collapse:collapse;width:100%;font-size:13px;margin-top:8px}
              th,td{border-bottom:1px solid #eee;padding:6px 8px;text-align:left;vertical-align:top}
              th{background:#fafafa}
              .critical{color:#b00020;font-weight:600} .high{color:#c25e00;font-weight:600}
              .medium{color:#8a6d00} .low{color:#555}
              .url{word-break:break-all;color:#0645ad}
              .fix{white-space:pre-line;min-width:280px}
            </style></head><body>
            """);

        sb.AppendLine($"<h1>{H(site.Name)}</h1>");
        sb.AppendLine($"<div class=\"sub\">{H(site.BaseUrl)} · {from:yyyy-MM-dd} – {to:yyyy-MM-dd} · tarama {crawl.Id}</div>");

        sb.AppendLine("<div class=\"cards\">");
        sb.AppendLine(Card("Genel skor", crawl.OverallScore?.ToString("0.0") ?? "—"));
        sb.AppendLine(Card("Taranan sayfa", crawl.PagesCrawled.ToString()));
        sb.AppendLine(Card("Bulunan adres", crawl.PagesDiscovered.ToString()));
        sb.AppendLine(Card("Bulgu", issues.Count.ToString()));
        sb.AppendLine(Card("Durum", crawl.Status.ToString()));
        sb.AppendLine("</div>");

        if (previous is not null)
        {
            var delta = crawl.OverallScore - previous.OverallScore;
            sb.AppendLine("<h2>Önceki taramaya göre</h2>");
            sb.AppendLine($"<p>Önceki skor: {previous.OverallScore?.ToString("0.0") ?? "—"} · "
                + $"değişim: {(delta is null ? "—" : delta.Value.ToString("+0.0;-0.0;0"))} · "
                + $"sayfa: {previous.PagesCrawled} → {crawl.PagesCrawled}</p>");
        }

        if (crawl.CategoryScores.Count > 0)
        {
            sb.AppendLine("<h2>Kategori skorları</h2><table><tr><th>Kategori</th><th>Skor</th></tr>");
            foreach (var (category, score) in crawl.CategoryScores.OrderBy(c => c.Key))
                sb.AppendLine($"<tr><td>{H(category)}</td><td>{score:0.0}</td></tr>");
            sb.AppendLine("</table>");
        }

        if (crawl.IssueCounts.Count > 0)
        {
            sb.AppendLine("<h2>Önem dağılımı</h2><table><tr><th>Önem</th><th>Adet</th></tr>");
            foreach (var (severity, count) in crawl.IssueCounts.OrderBy(c => c.Key))
                sb.AppendLine($"<tr><td class=\"{H(severity)}\">{H(severity)}</td><td>{count}</td></tr>");
            sb.AppendLine("</table>");
        }

        var grouped = issues
            .GroupBy(i => i.RuleCode)
            .OrderByDescending(g => g.Max(i => (int)i.Severity))
            .ThenByDescending(g => g.Count());

        sb.AppendLine("<h2>Kural bazlı özet</h2>");
        sb.AppendLine("<table><tr><th>Kural</th><th>Önem</th><th>Adet</th><th>Nasıl düzeltilir</th></tr>");
        foreach (var group in grouped)
        {
            var first = group.First();
            sb.AppendLine(
                $"<tr><td>{H(first.Rule?.TitleTr ?? first.RuleCode)}<br><small>{H(first.RuleCode)}</small></td>"
                + $"<td class=\"{Severity(first)}\">{Severity(first)}</td>"
                + $"<td>{group.Count()}</td>"
                + $"<td class=\"fix\">{H(FirstSteps(first.Rule?.HowToFixTr))}"
                + $"{DocLink(first.Rule?.DocUrl)}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine($"<h2>Bulgular (ilk {MaxIssuesListed})</h2>");
        sb.AppendLine("<table><tr><th>Önem</th><th>Kural</th><th>Sayfa</th><th>Kanıt</th></tr>");
        foreach (var issue in issues
            .OrderByDescending(i => i.Severity)
            .ThenBy(i => i.RuleCode)
            .Take(MaxIssuesListed))
        {
            sb.AppendLine(
                $"<tr><td class=\"{Severity(issue)}\">{Severity(issue)}</td>"
                + $"<td>{H(issue.RuleCode)}</td>"
                + $"<td class=\"url\">{H(issue.Page?.Url ?? "site geneli")}</td>"
                + $"<td>{H(issue.Evidence.Found)}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine($"<p class=\"sub\">Üretim: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC · SeoCopilot</p>");
        sb.AppendLine("</body></html>");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Card(string label, string value) =>
        $"<div class=\"card\">{H(label)}<b>{H(value)}</b></div>";

    private static string Severity(Issue issue) => issue.Severity.ToString().ToLowerInvariant();

    /// <summary>Ozet tablosunda gosterilen azami duzeltme adimi sayisi.</summary>
    private const int StepsInSummary = 2;

    /// <summary>
    /// Kural katalogundaki duzeltme adimlarinin ilk birkaci. Tam metin kural detay
    /// sayfasindadir; rapor ozet tablosuna kural basina ~1300 karakter sigmaz.
    /// </summary>
    private static string FirstSteps(string? howToFix)
    {
        if (string.IsNullOrWhiteSpace(howToFix)) return string.Empty;

        var steps = howToFix.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var shown = string.Join('\n', steps.Take(StepsInSummary));

        return steps.Length > StepsInSummary
            ? shown + $"\n… ({steps.Length - StepsInSummary} adım daha, kural detayında)"
            : shown;
    }

    /// <summary>Kuralin birincil kaynagi; seed'te tanimsizsa hicbir sey basilmaz.</summary>
    private static string DocLink(string? docUrl) => string.IsNullOrWhiteSpace(docUrl)
        ? string.Empty
        : $"<br><small><a class=\"url\" href=\"{H(docUrl)}\">Kaynak doküman</a></small>";

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
