using AngleSharp;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler;

/// <summary>
/// Playwright ile sayfayi render eder (JS dahil), sonra AngleSharp ile DOM'u ayristirir.
/// Şimdilik render adimi HttpClient fallback ile stub — Playwright entegrasyonu <see cref="PlaywrightBrowserPool"/>.
/// </summary>
public sealed class PageExtractor(HttpClient http) : IPageExtractor
{
    public async Task<ExtractedPage> ExtractAsync(string url, CancellationToken ct = default)
    {
        using var res = await http.GetAsync(url, ct);
        var status = (int)res.StatusCode;
        var html = await res.Content.ReadAsStringAsync(ct);

        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(html), ct);

        var title = doc.QuerySelector("title")?.TextContent?.Trim();
        var metaDesc = doc.QuerySelector("meta[name=description]")?.GetAttribute("content")?.Trim();
        var h1 = doc.QuerySelectorAll("h1").Select(e => e.TextContent.Trim()).Where(t => t.Length > 0).ToList();
        var host = new Uri(url).Host;
        var links = doc.QuerySelectorAll("a[href]")
            .Select(a => a.GetAttribute("href")!)
            .Where(h => h.StartsWith('/') || h.Contains(host, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
        var wordCount = (doc.Body?.TextContent ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var hasCanonical = doc.QuerySelector("link[rel=canonical]") is not null;

        return new ExtractedPage(url, status, title, metaDesc, h1, links, wordCount, hasCanonical);
    }
}
