using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;

namespace SeoCopilot.Crawler;

/// <summary>
/// HTML govdesini AngleSharp ile ayristirip pages tablosunu ve kural motorunu besleyen
/// alanlari cikarir. Nasil getirildiginden (HttpClient / Playwright) bagimsizdir.
/// </summary>
public sealed class HtmlAnalyzer(int maxMainTextChars)
{
    /// <summary>MainText hesaplanirken atilan, icerik tasimayan elemanlar.</summary>
    private const string NoiseSelector = "script, style, noscript, template, svg, nav, header, footer, aside, iframe";

    public async Task<ExtractedPage> AnalyzeAsync(
        string html,
        Uri requestUrl,
        Uri siteBaseUri,
        int statusCode,
        string? contentType,
        string? redirectTo,
        int responseTimeMs,
        int htmlSizeBytes,
        string? xRobotsTag,
        CancellationToken ct = default)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(html).Address(requestUrl.AbsoluteUri), ct);

        // Goreli linkler once <base href>, yoksa istek URL'ine gore cozulur.
        var linkBase = requestUrl;
        var baseHref = doc.QuerySelector("base[href]")?.GetAttribute("href");
        if (baseHref is not null && UrlNormalizer.TryNormalize(baseHref, requestUrl, out var declaredBase))
            linkBase = declaredBase;

        var title = doc.QuerySelector("title")?.TextContent?.Trim();
        var metaDescription = doc.QuerySelector("meta[name=description]")?.GetAttribute("content")?.Trim();
        var h1 = doc.QuerySelectorAll("h1")
            .Select(e => e.TextContent.Trim())
            .Where(t => t.Length > 0)
            .ToList();

        string? canonical = null;
        var canonicalHref = doc.QuerySelector("link[rel=canonical]")?.GetAttribute("href");
        if (canonicalHref is not null && UrlNormalizer.TryNormalize(canonicalHref, linkBase, out var canonicalUri))
            canonical = canonicalUri.AbsoluteUri;

        var images = doc.QuerySelectorAll("img").ToList();
        var links = ExtractLinks(doc, linkBase, siteBaseUri);
        var h2Count = doc.QuerySelectorAll("h2").Length;
        var robotsMeta = CombineRobots(doc.QuerySelector("meta[name=robots]")?.GetAttribute("content"), xRobotsTag);
        var ogDataJson = ExtractOpenGraph(doc);
        var schemaTypes = ExtractSchemaTypes(doc);
        var lang = doc.DocumentElement?.GetAttribute("lang")?.Trim();

        // DOM'u degistirdigi icin en son: script/nav/footer gibi elemanlari siler.
        var mainText = ExtractMainText(doc);

        return new ExtractedPage
        {
            Url = requestUrl.AbsoluteUri,
            StatusCode = statusCode,
            ContentType = contentType,
            RedirectTo = redirectTo,
            ResponseTimeMs = responseTimeMs,
            HtmlSizeBytes = htmlSizeBytes,
            Title = title,
            MetaDescription = metaDescription,
            H1 = h1,
            H2Count = h2Count,
            WordCount = CountWords(mainText),
            CanonicalUrl = canonical,
            RobotsMeta = robotsMeta,
            OgDataJson = ogDataJson,
            SchemaTypes = schemaTypes,
            ImagesTotal = images.Count,
            ImagesNoAlt = images.Count(i => string.IsNullOrWhiteSpace(i.GetAttribute("alt"))),
            MainText = Clip(mainText, maxMainTextChars),
            ContentHash = mainText.Length == 0 ? null : SHA256.HashData(Encoding.UTF8.GetBytes(mainText)),
            Lang = lang,
            Links = links
        };
    }

    private static List<ExtractedLink> ExtractLinks(IDocument doc, Uri linkBase, Uri siteBaseUri)
    {
        var links = new List<ExtractedLink>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var anchor in doc.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");
            if (!UrlNormalizer.TryNormalize(href, linkBase, out var url)) continue;
            if (!seen.Add(url.AbsoluteUri)) continue;

            var rel = anchor.GetAttribute("rel") ?? string.Empty;
            var anchorText = anchor.TextContent.Trim();

            links.Add(new ExtractedLink(
                url,
                anchorText.Length == 0 ? null : NormalizeWhitespace(anchorText),
                rel.Contains("nofollow", StringComparison.OrdinalIgnoreCase),
                UrlNormalizer.IsInternal(url, siteBaseUri)));
        }

        return links;
    }

    /// <summary>Gurultu elemanlari cikarilmis govde metni, tek bosluga normalize edilmis.</summary>
    private static string ExtractMainText(IDocument doc)
    {
        foreach (var noise in doc.QuerySelectorAll(NoiseSelector).ToList())
            noise.Remove();

        return NormalizeWhitespace(doc.Body?.TextContent ?? string.Empty);
    }

    private static string? ExtractOpenGraph(IDocument doc)
    {
        var og = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var meta in doc.QuerySelectorAll("meta[property^=\"og:\"]"))
        {
            var property = meta.GetAttribute("property");
            var content = meta.GetAttribute("content");
            if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(content)) continue;
            og.TryAdd(property.Trim(), content.Trim());
        }

        return og.Count == 0 ? null : JsonSerializer.Serialize(og);
    }

    private static List<string> ExtractSchemaTypes(IDocument doc)
    {
        var types = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var script in doc.QuerySelectorAll("script[type=\"application/ld+json\"]"))
        {
            var json = script.TextContent;
            if (string.IsNullOrWhiteSpace(json)) continue;
            try
            {
                using var parsed = JsonDocument.Parse(json);
                CollectSchemaTypes(parsed.RootElement, types, 0);
            }
            catch (JsonException)
            {
                // Bozuk JSON-LD yok sayilir.
            }
        }

        foreach (var element in doc.QuerySelectorAll("[itemtype]"))
        {
            var itemType = element.GetAttribute("itemtype");
            if (string.IsNullOrWhiteSpace(itemType)) continue;
            var name = itemType.TrimEnd('/').Split('/').LastOrDefault();
            if (!string.IsNullOrWhiteSpace(name)) types.Add(name);
        }

        return [.. types];
    }

    private static void CollectSchemaTypes(JsonElement element, ISet<string> sink, int depth)
    {
        if (depth > 8) return;

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("@type")) AddTypes(property.Value, sink);
                    else CollectSchemaTypes(property.Value, sink, depth + 1);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectSchemaTypes(item, sink, depth + 1);
                break;
        }
    }

    private static void AddTypes(JsonElement value, ISet<string> sink)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var name = value.GetString();
            if (!string.IsNullOrWhiteSpace(name)) sink.Add(name.Trim());
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray()) AddTypes(item, sink);
        }
    }

    private static string? CombineRobots(string? metaRobots, string? headerRobots)
    {
        var parts = new[] { metaRobots, headerRobots }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());
        var combined = string.Join(", ", parts);
        return combined.Length == 0 ? null : combined;
    }

    private static int CountWords(string text) =>
        text.Length == 0 ? 0 : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    private static string NormalizeWhitespace(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string? Clip(string value, int max) =>
        value.Length == 0 ? null : value.Length <= max ? value : value[..max];
}
