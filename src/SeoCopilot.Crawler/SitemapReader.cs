using System.Xml.Linq;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler;

/// <summary>sitemap.xml (ve sitemap index) icinden &lt;loc&gt; URL'lerini cikarir.</summary>
public sealed class SitemapReader(HttpClient http) : ISitemapSource
{
    private const int MaxDepth = 3;
    private const int MaxUrls = 50_000;

    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public Task<IReadOnlyList<string>> ReadAsync(string sitemapUrl, CancellationToken ct = default) =>
        ReadAsync(sitemapUrl, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase), ct);

    private async Task<IReadOnlyList<string>> ReadAsync(
        string sitemapUrl, int depth, HashSet<string> visited, CancellationToken ct)
    {
        if (depth > MaxDepth || !visited.Add(sitemapUrl)) return [];

        XDocument doc;
        try
        {
            var xml = await http.GetStringAsync(sitemapUrl, ct);
            doc = XDocument.Parse(xml);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Bozuk/erisilemeyen sitemap crawl'i dusurmez.
            return [];
        }

        // sitemap index ise alt sitemap'leri de gez
        var childSitemaps = doc.Descendants(Ns + "sitemap").Elements(Ns + "loc").Select(e => e.Value).ToList();
        if (childSitemaps.Count > 0)
        {
            var all = new List<string>();
            foreach (var child in childSitemaps)
            {
                if (all.Count >= MaxUrls) break;
                all.AddRange(await ReadAsync(child, depth + 1, visited, ct));
            }
            return Cap(all);
        }

        return Cap(doc.Descendants(Ns + "url").Elements(Ns + "loc").Select(e => e.Value.Trim()).ToList());
    }

    private static IReadOnlyList<string> Cap(List<string> urls) =>
        urls.Count > MaxUrls ? urls[..MaxUrls] : urls;
}
