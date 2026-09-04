using System.Net.Http.Headers;
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
///
/// Istenen adres ile belgenin adresi ayri tutulur: yonlendirme varsa goreli adresler
/// varilan URL'e gore cozulmelidir, yoksa <c>/en</c> → <c>/en/</c> gibi bir atlamada
/// sayfadaki <c>href="urun"</c> yanlislikla koke cozulur ve olmayan URL uretilir.
///
/// Govde string olarak degil ham bayt olarak alinir; karakter kodlamasini AngleSharp secer.
/// </summary>
public sealed class HtmlAnalyzer(int maxMainTextChars)
{
    static HtmlAnalyzer()
    {
        // .NET Core yalniz UTF-8/UTF-16/ASCII/Latin1 tasir. windows-1254 (Turkce),
        // windows-1251, iso-8859-9 gibi eski kod sayfalari bu saglayici olmadan
        // cozulemez ve AngleSharp UTF-8'e duserek ozel harfleri U+FFFD yapar.
        // Islem geneli, bir kez: HtmlAnalyzer'a ilk dokunusta calisir.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>MainText hesaplanirken atilan, icerik tasimayan elemanlar.</summary>
    private const string NoiseSelector = "script, style, noscript, template, svg, nav, header, footer, aside, iframe";

    /// <summary>Baslik hiyerarsisi sayilirken yok sayilan kapsayicilar — menu basliklari yanilgi yaratmasin.</summary>
    private const string ChromeSelector = "nav, header, footer, aside";

    /// <param name="body">
    /// Ham govde baytlari. Coz<em>ul</em>mus string degil: karakter kodlamasini AngleSharp
    /// secer (once <paramref name="bodyContentType"/>'daki charset, sonra &lt;meta charset&gt;,
    /// sonra BOM). Onceden UTF-8 varsayarak cozmek windows-1254 gibi sayfalarda butun
    /// Turkce harfleri yok ediyordu.
    /// </param>
    /// <param name="bodyContentType">
    /// <paramref name="body"/>'yi tanimlayan Content-Type — charset ipucu buradan gelir.
    /// Kayda gecen <paramref name="contentType"/>'dan ayri: Playwright yolunda govde zaten
    /// cozulmus gelir, orada bu "utf-8" olur ama sayfanin ilan ettigi charset baska olabilir.
    /// </param>
    /// <param name="requestUrl">
    /// Istenen adres. <c>pages.url</c> buraya yazilir — yonlendirme olsa da crawl'in
    /// kuyrugundaki kimlik budur.
    /// </param>
    /// <param name="documentUrl">
    /// Belgenin gercek adresi: yonlendirmeler izlendikten <em>sonra</em> varilan URL.
    /// Goreli adresler (link, canonical, gorsel) buna gore cozulur — RFC 3986 ve
    /// tarayicilar da boyle yapar. Yonlendirme yoksa <paramref name="requestUrl"/> ile aynidir.
    /// </param>
    public async Task<ExtractedPage> AnalyzeAsync(
        byte[] body,
        string? bodyContentType,
        Uri requestUrl,
        Uri documentUrl,
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

        using var stream = new MemoryStream(body, writable: false);
        var doc = await context.OpenAsync(
            req => req
                .Content(stream)
                .Header("Content-Type", NormalizeContentType(bodyContentType))
                .Address(documentUrl.AbsoluteUri),
            ct);

        // Goreli linkler once <base href>, yoksa belgenin adresine gore cozulur.
        var linkBase = documentUrl;
        var baseHref = doc.QuerySelector("base[href]")?.GetAttribute("href");
        if (baseHref is not null && UrlNormalizer.TryNormalize(baseHref, documentUrl, out var declaredBase))
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
        var headingLevels = ExtractHeadingLevels(doc);
        var robotsMeta = CombineRobots(doc.QuerySelector("meta[name=robots]")?.GetAttribute("content"), xRobotsTag);
        var openGraph = ExtractOpenGraph(doc);
        var (schemaTypes, invalidSchemaBlocks) = ExtractSchemaTypes(doc);
        var imageUrls = ExtractImageUrls(doc, images, linkBase);
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
            HeadingLevels = headingLevels,
            WordCount = CountWords(mainText),
            CanonicalUrl = canonical,
            RobotsMeta = robotsMeta,
            OgDataJson = openGraph.Json,
            OgTags = openGraph.Tags,
            SchemaTypes = schemaTypes,
            InvalidSchemaBlocks = invalidSchemaBlocks,
            ImagesTotal = images.Count,
            ImagesNoAlt = images.Count(i => string.IsNullOrWhiteSpace(i.GetAttribute("alt"))),
            ImageUrls = imageUrls,
            MainText = Clip(mainText, maxMainTextChars),
            ContentHash = mainText.Length == 0 ? null : SHA256.HashData(Encoding.UTF8.GetBytes(mainText)),
            Lang = lang,
            Links = links
        };
    }

    /// <summary>
    /// AngleSharp, Content-Type icindeki parametre adini harfe duyarli ariyor: <c>Charset=</c>
    /// (buyuk C) ile gelen charset'i gormeyip UTF-8'e dusuyor. Gercek sitelerde bu yazim
    /// yaygin, o yuzden basligi kucuk harfli <c>charset=</c> ile yeniden kuruyoruz.
    /// Cozulemeyen basligi oldugu gibi birakiriz — AngleSharp yine &lt;meta&gt;'ya bakabilir.
    /// </summary>
    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return "text/html";
        if (!MediaTypeHeaderValue.TryParse(contentType, out var parsed)) return contentType;

        var charset = parsed.CharSet?.Trim('"', ' ');
        var mediaType = string.IsNullOrWhiteSpace(parsed.MediaType) ? "text/html" : parsed.MediaType;

        return string.IsNullOrEmpty(charset) ? mediaType : $"{mediaType}; charset={charset}";
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

    /// <summary>Menu/altbilgi disindaki basliklarin belge sirasindaki seviyeleri.</summary>
    private static List<int> ExtractHeadingLevels(IDocument doc) =>
    [
        .. doc.QuerySelectorAll("h1, h2, h3, h4, h5, h6")
            .Where(e => e.Closest(ChromeSelector) is null)
            .Select(e => e.LocalName[1] - '0')
    ];

    /// <summary>Lazy-load temalarinin src yerine kullandigi nitelikler, oncelik sirasiyla.</summary>
    private static readonly string[] ImageSourceAttributes =
        ["src", "data-src", "data-lazy-src", "data-original", "data-echo"];

    private static readonly string[] ImageSrcSetAttributes = ["srcset", "data-srcset", "data-lazy-srcset"];

    /// <summary>
    /// Gorsel adresleri. Her &lt;img&gt; icin tarayicinin yukleyecegi <em>tek</em> adres alinir
    /// (src → data-src → srcset'in ilk adayı → kapsayan &lt;picture&gt; icindeki source),
    /// ustune LCP acisindan onemli olan preload gorselleri eklenir. Boylece srcset'li bir
    /// gorsel olcum butcesini tek basina tuketmez.
    /// </summary>
    private static List<string> ExtractImageUrls(IDocument doc, IEnumerable<IElement> images, Uri linkBase)
    {
        var urls = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string? candidate)
        {
            if (!UrlNormalizer.TryNormalize(candidate, linkBase, out var url)) return;
            if (seen.Add(url.AbsoluteUri)) urls.Add(url.AbsoluteUri);
        }

        foreach (var img in images)
            Add(PrimaryImageSource(img));

        foreach (var preload in doc.QuerySelectorAll("link[rel~=preload][as=image]"))
            Add(preload.GetAttribute("href") ?? FirstSrcSetCandidate(preload.GetAttribute("imagesrcset")));

        return urls;
    }

    private static string? PrimaryImageSource(IElement img)
    {
        foreach (var attribute in ImageSourceAttributes)
        {
            var value = img.GetAttribute(attribute);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        foreach (var attribute in ImageSrcSetAttributes)
        {
            if (FirstSrcSetCandidate(img.GetAttribute(attribute)) is string candidate) return candidate;
        }

        // <picture><source srcset=...><img> — img'de adres yoksa kardes source'a bak.
        var sources = img.Closest("picture")?.QuerySelectorAll("source") ?? Enumerable.Empty<IElement>();
        foreach (var source in sources)
        {
            foreach (var attribute in ImageSrcSetAttributes)
            {
                if (FirstSrcSetCandidate(source.GetAttribute(attribute)) is string candidate) return candidate;
            }
        }

        return null;
    }

    /// <summary>srcset ilk adayi: "a.jpg 1x, b.jpg 2x" → "a.jpg".</summary>
    private static string? FirstSrcSetCandidate(string? srcset)
    {
        if (string.IsNullOrWhiteSpace(srcset)) return null;

        foreach (var part in srcset.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = part.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrEmpty(candidate)) return candidate;
        }

        return null;
    }

    private static (string? Json, List<string> Tags) ExtractOpenGraph(IDocument doc)
    {
        var og = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var meta in doc.QuerySelectorAll("meta[property^=\"og:\"]"))
        {
            var property = meta.GetAttribute("property");
            var content = meta.GetAttribute("content");
            if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(content)) continue;
            og.TryAdd(property.Trim(), content.Trim());
        }

        return og.Count == 0
            ? (null, [])
            : (JsonSerializer.Serialize(og), [.. og.Keys]);
    }

    private static (List<string> Types, int InvalidBlocks) ExtractSchemaTypes(IDocument doc)
    {
        var types = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var invalidBlocks = 0;

        foreach (var script in doc.QuerySelectorAll("script[type=\"application/ld+json\"]"))
        {
            var json = script.TextContent;
            if (string.IsNullOrWhiteSpace(json)) continue;

            // Tam olarak bir kok deger = gecerli. 0 = hic okunamadi, >1 = tek script icinde
            // birden fazla kok nesne (JSON-LD'ye gore gecersiz; ayri script'lere bolunmeli).
            if (CollectJsonLdRoots(json, types) != 1) invalidBlocks++;
        }

        foreach (var element in doc.QuerySelectorAll("[itemtype]"))
        {
            var itemType = element.GetAttribute("itemtype");
            if (string.IsNullOrWhiteSpace(itemType)) continue;
            var name = itemType.TrimEnd('/').Split('/').LastOrDefault();
            if (!string.IsNullOrWhiteSpace(name)) types.Add(name);
        }

        return ([.. types], invalidBlocks);
    }

    /// <summary>
    /// Bir ld+json blogundaki kok degerleri sirayla okur ve tiplerini toplar; okunan kok
    /// sayisini doner.
    ///
    /// <c>JsonDocument.Parse</c> tek kok deger bekler. Bazi siteler tek script icine birden
    /// fazla nesneyi arka arkaya koyuyor; o durumda Parse "Additional text encountered"
    /// firlatiyor ve butun isaretleme sessizce kayboluyordu — sayfa "schema yok" sanilirdi.
    /// Burada hepsi okunur, gecersizlik ayrica bildirilir.
    /// </summary>
    private static int CollectJsonLdRoots(string json, ISet<string> sink)
    {
        var reader = new Utf8JsonReader(
            Encoding.UTF8.GetBytes(json),
            new JsonReaderOptions
            {
                // Bu bayrak olmadan okuyucu ilk kok degerden sonrasini reddeder.
                AllowMultipleValues = true,
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

        var roots = 0;
        try
        {
            while (reader.Read())
            {
                using var document = JsonDocument.ParseValue(ref reader);
                CollectSchemaTypes(document.RootElement, sink, 0);
                roots++;
            }
        }
        catch (JsonException)
        {
            // Kalanini okuyamadik; o ana kadar toplananlar duruyor.
        }

        return roots;
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
