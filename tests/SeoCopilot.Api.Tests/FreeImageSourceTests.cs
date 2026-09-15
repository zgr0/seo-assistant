using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Infrastructure.Media;
using SkiaSharp;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Ucretsiz gorsel kaynaklari: aday secimi, kirpma/kart cizimi ve ic ag korumasi.
/// Hicbiri disariya istek atmaz.
/// </summary>
public class FreeImageSourceTests
{
    private static readonly SkiaImageCanvas Canvas = new(NullLogger<SkiaImageCanvas>.Instance);

    private static byte[] Photo(int width, int height, SKEncodedImageFormat format = SKEncodedImageFormat.Jpeg,
        SKColor? fill = null)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(fill ?? new SKColor(90, 120, 150));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }

    // --- aday secimi ---

    [Theory]
    [InlineData("https://ornek.com/uploads/images/logo.png")]
    [InlineData("https://ornek.com/uploads/flags/Turkey.png")]
    [InlineData("https://ornek.com/img/icon-phone.png")]
    [InlineData("https://ornek.com/img/hero.svg")]
    [InlineData("https://ornek.com/img/loading.gif")]
    [InlineData("data:image/png;base64,AAAA")]
    public void Non_photos_are_rejected_by_name(string url) =>
        Assert.False(PageImageCandidates.LooksLikePhoto(url));

    [Theory]
    [InlineData("https://ornek.com/uploads/news/blog-ankiros-202427122024-180900.jpg")]
    [InlineData("https://cdn.ornek.com/urunler/a6-serisi-plastik-enjeksiyon-makinasi.png")]
    // Ust klasor ya da alan adindaki kalip gercek fotografi elememeli.
    [InlineData("https://logocdn.ornek.com/2024/archive/uploads/fabrika.jpg")]
    public void Photos_pass(string url) =>
        Assert.True(PageImageCandidates.LooksLikePhoto(url));

    [Fact]
    public void Og_image_comes_first_and_relative_paths_are_resolved()
    {
        var page = new Page
        {
            Url = "https://ornek.com/blog/fuar",
            OgData = """{"og:image":"/uploads/news/fuar-kapak.jpg"}""",
            ImageUrls = ["https://ornek.com/uploads/news/fuar-2.jpg"]
        };

        var candidates = PageImageCandidates.For(page, new HashSet<string>());

        Assert.Equal("https://ornek.com/uploads/news/fuar-kapak.jpg", candidates[0]);
        Assert.Equal("https://ornek.com/uploads/news/fuar-2.jpg", candidates[1]);
    }

    [Fact]
    public void Images_repeated_across_the_site_are_template_images()
    {
        // generalmakina.com.tr gibi: logo ve katalog afisi her sayfada, og:image her sayfada logo.
        const string catalog = "https://ornek.com/uploads/images/tanitim-katalog.jpg";
        var pages = Enumerable.Range(0, 6).Select(i => new Page
        {
            Url = $"https://ornek.com/sayfa-{i}",
            OgData = """{"og:image":"https://ornek.com/uploads/images/genel-kapak.jpg"}""",
            ImageUrls = [catalog, $"https://ornek.com/uploads/news/haber-{i}.jpg"]
        }).ToList();

        var siteWide = PageImageCandidates.SiteWideImages(pages);
        var candidates = PageImageCandidates.For(pages[2], siteWide);

        Assert.Contains(catalog, siteWide);
        Assert.Equal(["https://ornek.com/uploads/news/haber-2.jpg"], candidates);
    }

    [Fact]
    public void Candidate_count_is_capped()
    {
        var page = new Page
        {
            Url = "https://ornek.com/galeri",
            ImageUrls = [.. Enumerable.Range(0, 20).Select(i => $"https://ornek.com/foto-{i}.jpg")]
        };

        Assert.Equal(PageImageCandidates.MaxCandidates, PageImageCandidates.For(page, new HashSet<string>()).Count);
    }

    // --- cizim ---

    [Theory]
    [InlineData("1:1", 1080, 1080)]
    [InlineData("16:9", 1200, 675)]
    public void Photos_are_cropped_to_the_platform_size(string aspect, int width, int height)
    {
        var fitted = Canvas.Fit(Photo(1600, 1000), aspect);

        Assert.NotNull(fitted);
        Assert.Equal(width, fitted.Width);
        Assert.Equal(height, fitted.Height);

        using var decoded = SKBitmap.Decode(fitted.Content);
        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
    }

    [Theory]
    [InlineData(300, 300)]    // cok kucuk
    [InlineData(1200, 200)]   // logo seridi
    [InlineData(200, 1200)]   // dikey serit
    public void Unsuitable_images_are_refused(int width, int height) =>
        Assert.Null(Canvas.Fit(Photo(width, height), "1:1"));

    [Fact]
    public void Broken_bytes_are_refused() =>
        Assert.Null(Canvas.Fit([1, 2, 3], "1:1"));

    [Fact]
    public void Transparent_png_does_not_turn_black()
    {
        // Dekupe urun gorseli: tamamen seffaf zemin.
        var png = Photo(800, 800, SKEncodedImageFormat.Png, SKColors.Transparent);

        var fitted = Canvas.Fit(png, "1:1")!;

        using var decoded = SKBitmap.Decode(fitted.Content);
        var pixel = decoded.GetPixel(10, 10);
        Assert.True(pixel.Red > 200 && pixel.Green > 200 && pixel.Blue > 200, $"zemin koyu: {pixel}");
    }

    [Fact]
    public void Card_has_platform_size_and_is_stable_per_site()
    {
        var first = Canvas.Card("16:9", "generalmakina.com.tr");
        var again = Canvas.Card("16:9", "generalmakina.com.tr");
        var other = Canvas.Card("16:9", "baskasite.com");

        Assert.Equal((1200, 675), (first.Width, first.Height));
        Assert.Equal(first.Content, again.Content);

        using var a = SKBitmap.Decode(first.Content);
        using var b = SKBitmap.Decode(other.Content);
        Assert.NotEqual(a.GetPixel(20, 20), b.GetPixel(20, 20));
    }

    // --- ic ag korumasi ---

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.20.0.5")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.169.254")]   // bulut meta veri adresi
    [InlineData("100.64.0.1")]        // CGNAT
    [InlineData("0.0.0.0")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("::ffff:10.0.0.1")]   // IPv6 icine gomulu ozel IPv4
    public void Internal_addresses_are_blocked(string ip) =>
        Assert.True(SiteImageFetcher.IsNonPublic(IPAddress.Parse(ip)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("185.60.216.35")]
    [InlineData("2606:4700:4700::1111")]
    public void Public_addresses_are_allowed(string ip) =>
        Assert.False(SiteImageFetcher.IsNonPublic(IPAddress.Parse(ip)));

    [Fact]
    public async Task Fetcher_refuses_a_local_server_unless_explicitly_allowed()
    {
        await using var server = await ImageTestServer.StartAsync(Photo(1200, 900));
        var url = $"{server.BaseUrl}/uploads/fabrika.jpg";

        var blocked = NewFetcher(allowPrivate: false);
        Assert.Null(await blocked.FetchAsync(url));

        var allowed = NewFetcher(allowPrivate: true);
        var bytes = await allowed.FetchAsync(url);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000);
    }

    [Fact]
    public async Task Fetcher_rejects_non_images_and_oversized_files()
    {
        await using var server = await ImageTestServer.StartAsync(Photo(1200, 900));
        var fetcher = NewFetcher(allowPrivate: true, maxBytes: 500);

        Assert.Null(await fetcher.FetchAsync($"{server.BaseUrl}/sayfa.html"));
        Assert.Null(await fetcher.FetchAsync($"{server.BaseUrl}/uploads/fabrika.jpg")); // 500 bayti asar
        Assert.Null(await fetcher.FetchAsync("file:///etc/passwd"));
    }

    private static SiteImageFetcher NewFetcher(bool allowPrivate, int maxBytes = 8 * 1024 * 1024)
    {
        var options = new SiteImageFetcherOptions { AllowPrivateNetworks = allowPrivate, MaxBytes = maxBytes };
        return new SiteImageFetcher(
            new HttpClient(SiteImageFetcher.CreateHandler(options)),
            Options.Create(options),
            NullLogger<SiteImageFetcher>.Instance);
    }
}
