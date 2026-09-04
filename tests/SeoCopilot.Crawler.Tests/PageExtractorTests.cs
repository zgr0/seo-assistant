using System.Net;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler.Tests;

public class PageExtractorTests
{
    private static readonly Uri BaseUri = new("https://example.com/");
    private static readonly Uri PageUri = new("https://example.com/blog/yazi");

    private const string RichHtml = """
        <!doctype html>
        <html lang="tr">
          <head>
            <title>Ornek yazi basligi</title>
            <meta name="description" content="Kisa aciklama">
            <meta name="robots" content="index, follow">
            <link rel="canonical" href="/blog/yazi">
            <meta property="og:title" content="OG Baslik">
            <meta property="og:type" content="article">
            <script type="application/ld+json">
              {"@context":"https://schema.org","@type":"Article","author":{"@type":"Person"}}
            </script>
          </head>
          <body>
            <nav><a href="/menu">menu linki burada</a></nav>
            <h1>Ana baslik</h1>
            <h2>Alt baslik bir</h2>
            <h2>Alt baslik iki</h2>
            <p>govde metni bir iki uc</p>
            <img src="/a.png" alt="aciklama">
            <img src="/b.png">
            <img src="/c.png" alt="  ">
            <a href="../iletisim">iletisim</a>
            <a href="https://disari.com/x" rel="nofollow noopener">dis link</a>
            <a href="/blog/yazi#bolum">ayni sayfa</a>
            <script>gizli kelimeler sayilmamali</script>
            <footer>alt bilgi metni</footer>
          </body>
        </html>
        """;

    private static PageExtractor Build(StubHttpMessageHandler handler, CrawlerOptions? options = null) =>
        new(new HttpClient(handler), new PlaywrightBrowserPool(), Options.Create(options ?? new CrawlerOptions()));

    private static Task<ExtractedPage> ExtractAsync(StubHttpMessageHandler handler, Uri? url = null) =>
        Build(handler).ExtractAsync(url ?? PageUri, new PageFetchOptions(BaseUri));

    [Fact]
    public async Task Extracts_head_metadata()
    {
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, RichHtml);

        var page = await ExtractAsync(handler);

        Assert.Equal(200, page.StatusCode);
        Assert.Equal("Ornek yazi basligi", page.Title);
        Assert.Equal("Kisa aciklama", page.MetaDescription);
        Assert.Equal("tr", page.Lang);
        Assert.Equal("https://example.com/blog/yazi", page.CanonicalUrl);
        Assert.True(page.HasCanonical);
        Assert.Equal("index, follow", page.RobotsMeta);
        Assert.Equal(["Ana baslik"], page.H1);
        Assert.Equal(2, page.H2Count);
    }

    [Fact]
    public async Task Extracts_open_graph_and_schema_types()
    {
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, RichHtml);

        var page = await ExtractAsync(handler);

        Assert.NotNull(page.OgDataJson);
        Assert.Contains("og:title", page.OgDataJson);
        Assert.Contains("OG Baslik", page.OgDataJson);
        Assert.Equal(["og:title", "og:type"], page.OgTags.Order());
        Assert.Contains("Article", page.SchemaTypes);
        Assert.Contains("Person", page.SchemaTypes);
    }

    [Fact]
    public async Task Heading_levels_are_collected_in_document_order()
    {
        const string html = """
            <html lang="tr"><body>
              <header><h2>menu basligi</h2></header>
              <h1>Ana</h1><h2>Alt</h2><h4>Atlanmis</h4>
            </body></html>
            """;
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, html);

        var page = await ExtractAsync(handler);

        // header icindeki baslik sayilmaz
        Assert.Equal([1, 2, 4], page.HeadingLevels);
    }

    [Fact]
    public async Task Image_urls_are_absolutized_and_deduplicated()
    {
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, RichHtml);

        var page = await ExtractAsync(handler);

        Assert.Equal(
            ["https://example.com/a.png", "https://example.com/b.png", "https://example.com/c.png"],
            page.ImageUrls);
    }

    [Fact]
    public async Task Image_sizes_are_measured_with_head_requests()
    {
        var handler = new StubHttpMessageHandler()
            .Map(PageUri.AbsoluteUri, RichHtml)
            .Map("https://example.com/a.png", new string('x', 300_000), "image/png")
            .Map("https://example.com/b.png", new string('x', 1_000), "image/png");
        // /c.png eslesmiyor → 404 → olculemez

        var page = await ExtractAsync(handler);

        Assert.Equal(2, page.ImageSizes.Count);
        Assert.Equal(300_000, page.ImageSizes.Single(i => i.Url.EndsWith("a.png")).Bytes);
        Assert.Contains(handler.Requests, r => r == "https://example.com/a.png");
    }

    [Fact]
    public async Task Image_size_checks_can_be_switched_off()
    {
        var handler = new StubHttpMessageHandler()
            .Map(PageUri.AbsoluteUri, RichHtml)
            .Map("https://example.com/a.png", new string('x', 300_000), "image/png");

        var page = await Build(handler, new CrawlerOptions { MaxImageChecksPerPage = 0 })
            .ExtractAsync(PageUri, new PageFetchOptions(BaseUri));

        Assert.Empty(page.ImageSizes);
        Assert.Single(handler.Requests); // yalniz sayfanin kendisi
    }

    [Fact]
    public async Task Counts_images_without_alt()
    {
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, RichHtml);

        var page = await ExtractAsync(handler);

        Assert.Equal(3, page.ImagesTotal);
        Assert.Equal(2, page.ImagesNoAlt); // alt yok + alt bos
    }

    [Fact]
    public async Task Links_are_absolutized_classified_and_deduplicated()
    {
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, RichHtml);

        var page = await ExtractAsync(handler);
        var urls = page.Links.Select(l => l.Url.AbsoluteUri).ToList();

        Assert.Contains("https://example.com/menu", urls);
        Assert.Contains("https://example.com/iletisim", urls);
        Assert.Contains("https://disari.com/x", urls);

        // fragment atildigi icin kendi URL'ine dusen link ayri sayilmaz
        Assert.Equal(urls.Count, urls.Distinct().Count());

        var external = Assert.Single(page.Links, l => !l.IsInternal);
        Assert.True(external.IsNofollow);
        Assert.Equal("dis link", external.AnchorText);

        Assert.All(page.Links.Where(l => l.IsInternal), l => Assert.False(l.IsNofollow));
    }

    [Fact]
    public async Task Word_count_ignores_scripts_and_chrome()
    {
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, RichHtml);

        var page = await ExtractAsync(handler);

        Assert.NotNull(page.MainText);
        Assert.DoesNotContain("gizli", page.MainText);
        Assert.DoesNotContain("menu linki", page.MainText);
        Assert.DoesNotContain("alt bilgi", page.MainText);
        Assert.Contains("govde metni", page.MainText);

        // "Ana baslik" + 2 x "Alt baslik x" + "govde metni bir iki uc" + "iletisim" + ...
        Assert.True(page.WordCount > 0);
        Assert.Equal(page.MainText!.Split(' ').Length, page.WordCount);
    }

    [Fact]
    public async Task Content_hash_matches_for_identical_bodies()
    {
        var handler = new StubHttpMessageHandler()
            .Map("https://example.com/a", RichHtml)
            .Map("https://example.com/b", RichHtml)
            .Map("https://example.com/c", RichHtml.Replace("govde metni bir iki uc", "bambaska bir govde"));

        var extractor = Build(handler);
        var options = new PageFetchOptions(BaseUri);

        var a = await extractor.ExtractAsync(new Uri("https://example.com/a"), options);
        var b = await extractor.ExtractAsync(new Uri("https://example.com/b"), options);
        var c = await extractor.ExtractAsync(new Uri("https://example.com/c"), options);

        Assert.Equal(a.ContentHash, b.ContentHash);
        Assert.NotEqual(a.ContentHash, c.ContentHash);
    }

    [Fact]
    public async Task Base_href_wins_for_relative_links()
    {
        const string html = """
            <html><head><base href="https://example.com/kok/"></head>
            <body><a href="sayfa">x</a></body></html>
            """;
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, html);

        var page = await ExtractAsync(handler);

        Assert.Equal("https://example.com/kok/sayfa", page.Links[0].Url.AbsoluteUri);
    }

    [Fact]
    public async Task Redirect_chain_is_followed_and_recorded()
    {
        var handler = new StubHttpMessageHandler()
            .MapRedirect("https://example.com/eski", "https://example.com/yeni")
            .Map("https://example.com/yeni", RichHtml);

        var page = await ExtractAsync(handler, new Uri("https://example.com/eski"));

        Assert.Equal(200, page.StatusCode);
        Assert.Equal("https://example.com/yeni", page.RedirectTo);
        Assert.Equal("https://example.com/eski", page.Url);
        Assert.Equal(1, page.RedirectCount);
    }

    [Fact]
    public async Task Several_json_ld_roots_in_one_script_are_all_read()
    {
        // Gercek vaka: tek script icinde iki kok nesne arka arkaya. JSON-LD'ye gore gecersiz
        // ama yaygin; JsonDocument.Parse patlayip butun isaretlemeyi sessizce dusuruyordu.
        const string html = """
            <html lang="tr"><head><script type="application/ld+json">
            {"@context":"http://schema.org","@type":"Organization","name":"Ornek"}
            {"@context":"http://schema.org","@type":"Person","name":"Biri"}
            </script></head><body><h1>x</h1></body></html>
            """;
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, html);

        var page = await ExtractAsync(handler);

        Assert.Contains("Organization", page.SchemaTypes);
        Assert.Contains("Person", page.SchemaTypes);

        // Okundu ama bicim gecersiz — ayri bulgu uretilmeli.
        Assert.Equal(1, page.InvalidSchemaBlocks);
    }

    [Fact]
    public async Task Well_formed_json_ld_is_not_flagged()
    {
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, RichHtml);

        var page = await ExtractAsync(handler);

        Assert.Contains("Article", page.SchemaTypes);
        Assert.Equal(0, page.InvalidSchemaBlocks);
    }

    [Fact]
    public async Task Broken_json_ld_is_flagged_and_yields_no_types()
    {
        const string html = """
            <html lang="tr"><head><script type="application/ld+json">
            {"@type":"Article", bozuk
            </script></head><body><h1>x</h1></body></html>
            """;
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, html);

        var page = await ExtractAsync(handler);

        Assert.Empty(page.SchemaTypes);
        Assert.Equal(1, page.InvalidSchemaBlocks);
    }

    [Fact]
    public async Task Redirect_to_a_non_http_target_is_reported_not_swallowed()
    {
        // Gercek vaka: `Location: javascript:;`. Istek denenirse istisna cikar ve sayfa
        // "getirilemedi" (status 0) gorunurdu — oysa sunucu duzgun 301 dondu.
        var handler = new StubHttpMessageHandler()
            .MapRedirect("https://example.com/kurumsal", "javascript:;");

        var page = await ExtractAsync(handler, new Uri("https://example.com/kurumsal"));

        Assert.Equal(301, page.StatusCode);
        Assert.Equal("javascript:;", page.InvalidRedirectTarget);
        Assert.Null(page.RedirectTo);

        // Zincir orada kesilir; hedefe istek atilmaz.
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Relative_links_resolve_against_the_redirected_url()
    {
        // Gercek vaka: /en → /en/ atlamasi, sayfadaki linkler slash'siz goreli.
        // Taban olarak /en alinirsa "a5-series" koke cozulur ve olmayan URL uretilir.
        const string html = """
            <html lang="tr"><body>
              <a href="a5-series">urun</a>
              <a href="automotive">sektor</a>
            </body></html>
            """;
        var handler = new StubHttpMessageHandler()
            .MapRedirect("https://example.com/en", "https://example.com/en/")
            .Map("https://example.com/en/", html);

        var page = await ExtractAsync(handler, new Uri("https://example.com/en"));

        Assert.Equal(
            ["https://example.com/en/a5-series", "https://example.com/en/automotive"],
            page.Links.Select(l => l.Url.AbsoluteUri));

        // Kimlik degismez: pages.url istenen adres, varilan adres redirect_to'da.
        Assert.Equal("https://example.com/en", page.Url);
        Assert.Equal("https://example.com/en/", page.RedirectTo);
    }

    [Fact]
    public async Task Canonical_and_image_urls_follow_the_redirected_url()
    {
        const string html = """
            <html lang="tr">
              <head><link rel="canonical" href="."></head>
              <body><img src="logo.png" alt="logo"></body>
            </html>
            """;
        var handler = new StubHttpMessageHandler()
            .MapRedirect("https://example.com/en", "https://example.com/en/")
            .Map("https://example.com/en/", html);

        var page = await ExtractAsync(handler, new Uri("https://example.com/en"));

        Assert.Equal("https://example.com/en/", page.CanonicalUrl);
        Assert.Equal(["https://example.com/en/logo.png"], page.ImageUrls);
    }

    [Fact]
    public async Task Relative_base_href_is_resolved_against_the_redirected_url()
    {
        const string html = """
            <html><head><base href="kok/"></head>
            <body><a href="sayfa">x</a></body></html>
            """;
        var handler = new StubHttpMessageHandler()
            .MapRedirect("https://example.com/en", "https://example.com/en/")
            .Map("https://example.com/en/", html);

        var page = await ExtractAsync(handler, new Uri("https://example.com/en"));

        Assert.Equal("https://example.com/en/kok/sayfa", page.Links[0].Url.AbsoluteUri);
    }

    [Fact]
    public async Task Redirect_count_grows_with_every_hop()
    {
        var handler = new StubHttpMessageHandler()
            .MapRedirect("https://example.com/1", "https://example.com/2")
            .MapRedirect("https://example.com/2", "https://example.com/3")
            .Map("https://example.com/3", RichHtml);

        var page = await ExtractAsync(handler, new Uri("https://example.com/1"));

        Assert.Equal(2, page.RedirectCount);
    }

    [Fact]
    public async Task Redirect_loop_stops_at_the_hop_limit()
    {
        var handler = new StubHttpMessageHandler()
            .MapRedirect("https://example.com/a", "https://example.com/b")
            .MapRedirect("https://example.com/b", "https://example.com/a");

        var page = await ExtractAsync(handler, new Uri("https://example.com/a"));

        Assert.Equal(301, page.StatusCode);
        Assert.Equal(6, handler.Requests.Count); // ilk istek + MaxRedirects (5) atlama
    }

    [Fact]
    public async Task Non_html_content_is_recorded_but_not_parsed()
    {
        var handler = new StubHttpMessageHandler().Map(
            "https://example.com/rapor.pdf", "%PDF-1.7", "application/pdf");

        var page = await ExtractAsync(handler, new Uri("https://example.com/rapor.pdf"));

        Assert.Equal(200, page.StatusCode);
        Assert.Contains("application/pdf", page.ContentType);
        Assert.Empty(page.Links);
        Assert.Null(page.Title);
        Assert.Null(page.ContentHash);
    }

    [Fact]
    public async Task Error_pages_are_recorded_without_link_harvesting()
    {
        var handler = new StubHttpMessageHandler().Map(
            "https://example.com/yok", RichHtml, status: HttpStatusCode.NotFound);

        var page = await ExtractAsync(handler, new Uri("https://example.com/yok"));

        Assert.Equal(404, page.StatusCode);
        Assert.Empty(page.Links);
    }

    [Fact]
    public async Task X_robots_tag_header_is_merged_into_robots_meta()
    {
        var handler = new StubHttpMessageHandler().MapHeader(
            PageUri.AbsoluteUri, RichHtml, "X-Robots-Tag", "noindex");

        var page = await ExtractAsync(handler);

        Assert.NotNull(page.RobotsMeta);
        Assert.Contains("noindex", page.RobotsMeta);
        Assert.Contains("index, follow", page.RobotsMeta);
    }

    [Fact]
    public async Task Html_body_is_capped_at_the_configured_size()
    {
        var big = "<html><body>" + new string('x', 50_000) + "</body></html>";
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, big);

        var page = await Build(handler, new CrawlerOptions { MaxHtmlBytes = 1024 })
            .ExtractAsync(PageUri, new PageFetchOptions(BaseUri));

        Assert.Equal(1024, page.HtmlSizeBytes);
    }
}
