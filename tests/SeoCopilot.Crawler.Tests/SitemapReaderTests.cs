namespace SeoCopilot.Crawler.Tests;

public class SitemapReaderTests
{
    private const string Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static string UrlSet(params string[] urls) =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <urlset xmlns="{Ns}">
          {string.Join("\n  ", urls.Select(u => $"<url><loc>{u}</loc></url>"))}
        </urlset>
        """;

    private static string Index(params string[] sitemaps) =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <sitemapindex xmlns="{Ns}">
          {string.Join("\n  ", sitemaps.Select(s => $"<sitemap><loc>{s}</loc></sitemap>"))}
        </sitemapindex>
        """;

    private static SitemapReader Reader(StubHttpMessageHandler handler) => new(new HttpClient(handler));

    [Fact]
    public async Task Reads_plain_url_set()
    {
        var handler = new StubHttpMessageHandler().Map(
            "https://example.com/sitemap.xml",
            UrlSet("https://example.com/a", "https://example.com/b"),
            "application/xml");

        var urls = await Reader(handler).ReadAsync("https://example.com/sitemap.xml");

        Assert.Equal(["https://example.com/a", "https://example.com/b"], urls);
    }

    [Fact]
    public async Task Follows_sitemap_index()
    {
        var handler = new StubHttpMessageHandler()
            .Map("https://example.com/sitemap.xml",
                Index("https://example.com/s1.xml", "https://example.com/s2.xml"), "application/xml")
            .Map("https://example.com/s1.xml", UrlSet("https://example.com/a"), "application/xml")
            .Map("https://example.com/s2.xml", UrlSet("https://example.com/b"), "application/xml");

        var urls = await Reader(handler).ReadAsync("https://example.com/sitemap.xml");

        Assert.Equal(2, urls.Count);
        Assert.Contains("https://example.com/a", urls);
        Assert.Contains("https://example.com/b", urls);
    }

    [Fact]
    public async Task Self_referencing_index_does_not_loop()
    {
        var handler = new StubHttpMessageHandler().Map(
            "https://example.com/sitemap.xml",
            Index("https://example.com/sitemap.xml"),
            "application/xml");

        var urls = await Reader(handler).ReadAsync("https://example.com/sitemap.xml");

        Assert.Empty(urls);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Broken_xml_yields_empty_list()
    {
        var handler = new StubHttpMessageHandler().Map(
            "https://example.com/sitemap.xml", "<urlset><url>", "application/xml");

        Assert.Empty(await Reader(handler).ReadAsync("https://example.com/sitemap.xml"));
    }

    [Fact]
    public async Task Missing_sitemap_yields_empty_list()
    {
        var handler = new StubHttpMessageHandler();

        Assert.Empty(await Reader(handler).ReadAsync("https://example.com/sitemap.xml"));
    }
}
