using System.Xml.Linq;

namespace SeoCopilot.Crawler;

/// <summary>sitemap.xml (ve sitemap index) icinden <loc> URL'lerini cikarir.</summary>
public sealed class SitemapReader(HttpClient http)
{
    public async Task<IReadOnlyList<string>> ReadAsync(string sitemapUrl, CancellationToken ct = default)
    {
        var xml = await http.GetStringAsync(sitemapUrl, ct);
        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        // sitemap index ise alt sitemap'leri de gez
        var childSitemaps = doc.Descendants(ns + "sitemap").Elements(ns + "loc").Select(e => e.Value).ToList();
        if (childSitemaps.Count > 0)
        {
            var all = new List<string>();
            foreach (var child in childSitemaps)
                all.AddRange(await ReadAsync(child, ct));
            return all;
        }

        return doc.Descendants(ns + "url").Elements(ns + "loc").Select(e => e.Value).ToList();
    }
}
