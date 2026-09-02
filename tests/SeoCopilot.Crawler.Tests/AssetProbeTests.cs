using System.Net;
using Microsoft.Extensions.Options;

namespace SeoCopilot.Crawler.Tests;

/// <summary>
/// Ikili varliklar (pdf, zip, jpg...) sayfa gibi taranmaz; yalniz durumu yoklanir.
/// Govde indirilmedigi icin HEAD tercih edilir.
/// </summary>
public class AssetProbeTests
{
    private static PageExtractor Build(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler), new PlaywrightBrowserPool(), Options.Create(new CrawlerOptions()));

    [Fact]
    public async Task Probe_uses_head_and_reports_status_with_content_type()
    {
        var handler = new StubHttpMessageHandler()
            .Map("https://example.com/katalog.pdf", "%PDF-1.7", "application/pdf");

        var page = await Build(handler).ProbeAsync(new Uri("https://example.com/katalog.pdf"));

        Assert.Equal(200, page.StatusCode);
        Assert.Contains("application/pdf", page.ContentType);
        Assert.Equal(("HEAD", "https://example.com/katalog.pdf"), Assert.Single(handler.Calls));

        // Sayfa alanlari doldurulmaz — bu bir sayfa degil.
        Assert.Null(page.Title);
        Assert.Empty(page.Links);
        Assert.Null(page.ContentHash);
    }

    [Fact]
    public async Task Probe_falls_back_to_get_when_head_is_rejected()
    {
        var handler = new StubHttpMessageHandler()
            .MapHeadNotAllowed("https://example.com/rapor.pdf", "%PDF-1.7", "application/pdf");

        var page = await Build(handler).ProbeAsync(new Uri("https://example.com/rapor.pdf"));

        Assert.Equal(200, page.StatusCode);
        Assert.Equal(["HEAD", "GET"], handler.Calls.Select(c => c.Method));
    }

    [Fact]
    public async Task Probe_reports_missing_asset()
    {
        var handler = new StubHttpMessageHandler();

        var page = await Build(handler).ProbeAsync(new Uri("https://example.com/yok.pdf"));

        Assert.Equal(404, page.StatusCode);
    }

    [Fact]
    public async Task Probe_follows_redirects()
    {
        var handler = new StubHttpMessageHandler()
            .MapRedirect("https://example.com/eski.pdf", "https://example.com/yeni.pdf", HttpStatusCode.Found)
            .Map("https://example.com/yeni.pdf", "%PDF", "application/pdf");

        var page = await Build(handler).ProbeAsync(new Uri("https://example.com/eski.pdf"));

        Assert.Equal(200, page.StatusCode);
        Assert.Equal("https://example.com/yeni.pdf", page.RedirectTo);
    }
}
