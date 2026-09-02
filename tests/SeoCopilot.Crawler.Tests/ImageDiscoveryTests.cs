using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler.Tests;

/// <summary>
/// Modern temalarda gorsellerin cogu src'de degil srcset / data-src / picture icinde durur.
/// Her gorsel icin tek adres alinir ki olcum butcesi tek bir srcset'e harcanmasin.
/// </summary>
public class ImageDiscoveryTests
{
    private static readonly Uri BaseUri = new("https://example.com/");
    private static readonly Uri PageUri = new("https://example.com/sayfa");

    private const string Html = """
        <!doctype html>
        <html lang="tr">
          <head>
            <link rel="preload" as="image" href="/hero.jpg">
          </head>
          <body>
            <img src="/a.png" alt="a">
            <img data-src="/lazy.png" alt="lazy">
            <img srcset="/s1.png 1x, /s2.png 2x" alt="srcset">
            <img data-srcset="/d1.png 480w, /d2.png 960w" alt="data-srcset">
            <picture>
              <source srcset="/p1.webp" type="image/webp">
              <img alt="picture">
            </picture>
            <img src="/a.png" alt="tekrar">
            <img src="data:image/png;base64,iVBORw0KGgo=" alt="gomulu">
          </body>
        </html>
        """;

    private static async Task<ExtractedPage> ExtractAsync()
    {
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, Html);
        var extractor = new PageExtractor(
            new HttpClient(handler),
            new PlaywrightBrowserPool(),
            Options.Create(new CrawlerOptions { MaxImageChecksPerPage = 0 }));

        return await extractor.ExtractAsync(PageUri, new PageFetchOptions(BaseUri));
    }

    [Fact]
    public async Task Collects_one_url_per_image_across_every_source_form()
    {
        var page = await ExtractAsync();

        Assert.Equal(
            [
                "https://example.com/a.png",
                "https://example.com/lazy.png",
                "https://example.com/s1.png",
                "https://example.com/d1.png",
                "https://example.com/p1.webp",
                "https://example.com/hero.jpg"
            ],
            page.ImageUrls);
    }

    [Fact]
    public async Task Srcset_variants_do_not_flood_the_list()
    {
        var page = await ExtractAsync();

        Assert.DoesNotContain("https://example.com/s2.png", page.ImageUrls);
        Assert.DoesNotContain("https://example.com/d2.png", page.ImageUrls);
    }

    [Fact]
    public async Task Embedded_and_duplicate_sources_are_skipped()
    {
        var page = await ExtractAsync();

        Assert.DoesNotContain(page.ImageUrls, u => u.StartsWith("data:"));
        Assert.Equal(page.ImageUrls.Count, page.ImageUrls.Distinct().Count());
    }

    [Fact]
    public async Task Alt_counting_still_follows_img_elements()
    {
        var page = await ExtractAsync();

        Assert.Equal(7, page.ImagesTotal); // picture icindeki img ve data: URI dahil, source haric
        Assert.Equal(0, page.ImagesNoAlt);
    }
}
