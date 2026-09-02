using System.Diagnostics;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;

namespace SeoCopilot.Crawler.Tests;

/// <summary>
/// Gorsel boyutu olcumu sayfa disi ek istek uretir; robots.txt'ye uymasi ve nezaket
/// beklemesine tabi olmasi gerekir.
/// </summary>
public class ImageMeasurementTests
{
    private static readonly Uri BaseUri = new("https://example.com/");
    private static readonly Uri PageUri = new("https://example.com/sayfa");

    private const string ImageHtml = """
        <html lang="tr"><body>
          <img src="/gizli/logo.png" alt="gizli">
          <img src="/acik.png" alt="acik">
        </body></html>
        """;

    private static PageExtractor Build(StubHttpMessageHandler handler, CrawlerOptions? options = null) =>
        new(new HttpClient(handler), new PlaywrightBrowserPool(), Options.Create(options ?? new CrawlerOptions()));

    private static IRobotsPolicy Robots(string content) => RobotsTxt.Parse(content);

    [Fact]
    public async Task Robots_blocked_images_are_not_measured()
    {
        var handler = new StubHttpMessageHandler()
            .Map(PageUri.AbsoluteUri, ImageHtml)
            .Map("https://example.com/gizli/logo.png", new string('x', 300_000), "image/png")
            .Map("https://example.com/acik.png", new string('x', 300_000), "image/png");

        var robots = Robots("""
            User-agent: *
            Disallow: /gizli
            """);

        var page = await Build(handler).ExtractAsync(PageUri, new PageFetchOptions(BaseUri, Robots: robots));

        Assert.Equal("https://example.com/acik.png", Assert.Single(page.ImageSizes).Url);
        Assert.DoesNotContain(handler.Requests, r => r.Contains("/gizli/"));
    }

    [Fact]
    public async Task Cross_origin_images_skip_the_robots_check()
    {
        const string html = """
            <html lang="tr"><body><img src="https://cdn.example/logo.png" alt="cdn"></body></html>
            """;
        var handler = new StubHttpMessageHandler()
            .Map(PageUri.AbsoluteUri, html)
            .Map("https://cdn.example/logo.png", new string('x', 1000), "image/png");

        // Kendi robots'umuz her seyi kapatiyor ama CDN'in politikasi bizi baglamaz.
        var page = await Build(handler)
            .ExtractAsync(PageUri, new PageFetchOptions(BaseUri, Robots: Robots("User-agent: *\nDisallow: /")));

        Assert.Equal("https://cdn.example/logo.png", Assert.Single(page.ImageSizes).Url);
    }

    [Fact]
    public async Task Measurement_takes_permits_from_the_pacer()
    {
        var handler = new StubHttpMessageHandler()
            .Map(PageUri.AbsoluteUri, ImageHtml)
            .Map("https://example.com/gizli/logo.png", "x", "image/png")
            .Map("https://example.com/acik.png", "x", "image/png");

        using var pacer = new RequestPacer(concurrency: 1, delayMs: 100);

        var stopwatch = Stopwatch.StartNew();
        await Build(handler).ExtractAsync(PageUri, new PageFetchOptions(BaseUri, Pacer: pacer));
        stopwatch.Stop();

        // Iki gorsel iki izin ister: ilki hazir, ikincisi bir aralik bekler.
        Assert.True(stopwatch.ElapsedMilliseconds >= 90, $"beklenen >=90ms, olculen {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Cached_image_size_takes_no_permit()
    {
        var handler = new StubHttpMessageHandler()
            .Map("https://example.com/bir", ImageHtml)
            .Map("https://example.com/iki", ImageHtml)
            .Map("https://example.com/gizli/logo.png", "x", "image/png")
            .Map("https://example.com/acik.png", "x", "image/png");

        using var pacer = new RequestPacer(concurrency: 1, delayMs: 100);
        var extractor = Build(handler);

        await extractor.ExtractAsync(new Uri("https://example.com/bir"), new PageFetchOptions(BaseUri, Pacer: pacer));

        // Ikinci sayfada gorseller onbellekten gelir → nezaket borcu dogurmaz.
        var stopwatch = Stopwatch.StartNew();
        await extractor.ExtractAsync(new Uri("https://example.com/iki"), new PageFetchOptions(BaseUri, Pacer: pacer));
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds < 90, $"beklenen <90ms, olculen {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Cached_image_size_costs_no_second_request_and_no_wait()
    {
        var handler = new StubHttpMessageHandler()
            .Map("https://example.com/bir", ImageHtml)
            .Map("https://example.com/iki", ImageHtml)
            .Map("https://example.com/gizli/logo.png", "x", "image/png")
            .Map("https://example.com/acik.png", "x", "image/png");

        var extractor = Build(handler);
        var options = new PageFetchOptions(BaseUri);

        await extractor.ExtractAsync(new Uri("https://example.com/bir"), options);
        var before = handler.Requests.Count;
        await extractor.ExtractAsync(new Uri("https://example.com/iki"), options);

        // Ikinci sayfada yalniz sayfanin kendisi istendi; gorseller onbellekten geldi.
        Assert.Equal(before + 1, handler.Requests.Count);
    }

    [Fact]
    public async Task Measurement_uses_head_requests()
    {
        var handler = new StubHttpMessageHandler()
            .Map(PageUri.AbsoluteUri, ImageHtml)
            .Map("https://example.com/acik.png", "x", "image/png")
            .Map("https://example.com/gizli/logo.png", "x", "image/png");

        await Build(handler).ExtractAsync(PageUri, new PageFetchOptions(BaseUri));

        Assert.All(
            handler.Calls.Where(c => c.Url.EndsWith(".png")),
            c => Assert.Equal("HEAD", c.Method));
    }
}
