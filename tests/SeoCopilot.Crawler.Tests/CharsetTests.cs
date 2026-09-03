using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler.Tests;

/// <summary>
/// Govde UTF-8 varsayilarak cozulurse UTF-8 olmayan sayfalarda her ozel harf bozulur.
/// Gercek vaka: windows-1254 servis eden bir sitede butun Turkce harfler yok olmus,
/// ustelik her harf 3 karaktere sisip title uzunlugu kurallarini da yanlis tetiklemisti.
/// </summary>
public class CharsetTests
{
    private static readonly Uri BaseUri = new("https://example.com/");
    private static readonly Uri PageUri = new("https://example.com/urun");

    private static PageExtractor Build(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler), new PlaywrightBrowserPool(), Options.Create(new CrawlerOptions()));

    /// <summary>
    /// Govdeyi windows-1254 baytlarina cevirir. Kod sayfasi saglayicisina bagimli kalmamak
    /// icin yalniz testte gecen Turkce harfler elle eslenir; gerisi ASCII.
    /// </summary>
    private static byte[] Windows1254(string text)
    {
        var map = new Dictionary<char, byte>
        {
            ['ı'] = 0xFD, ['İ'] = 0xDD, ['ş'] = 0xFE, ['Ş'] = 0xDE,
            ['ğ'] = 0xF0, ['Ğ'] = 0xD0, ['ç'] = 0xE7, ['Ç'] = 0xC7,
            ['ö'] = 0xF6, ['Ö'] = 0xD6, ['ü'] = 0xFC, ['Ü'] = 0xDC
        };

        var bytes = new byte[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (map.TryGetValue(c, out var mapped)) bytes[i] = mapped;
            else if (c < 128) bytes[i] = (byte)c;
            else throw new ArgumentException($"'{c}' icin windows-1254 eslemesi yok", nameof(text));
        }
        return bytes;
    }

    private const string TurkishHtml = """
        <html lang="tr">
          <head>
            <title>Plastik Enjeksiyon Makinası</title>
            <meta name="description" content="Sıcak yolluk sistemli kalıplama çözümü.">
          </head>
          <body>
            <h1>Enjeksiyon Makinası</h1>
            <p>Yüksek basınçlı üretim için geliştirilmiş çözüm.</p>
            <a href="/kalıp">kalıp sayfası</a>
          </body>
        </html>
        """;

    [Theory]
    [InlineData("text/html; charset=windows-1254")]
    [InlineData("text/html; Charset=windows-1254")] // gercek sitede buyuk C ile geliyor
    [InlineData("text/html;charset=windows-1254")]
    public async Task Charset_from_the_content_type_header_is_honoured(string contentType)
    {
        var handler = new StubHttpMessageHandler()
            .MapBytes(PageUri.AbsoluteUri, Windows1254(TurkishHtml), contentType);

        var page = await Build(handler).ExtractAsync(PageUri, new PageFetchOptions(BaseUri));

        Assert.Equal("Plastik Enjeksiyon Makinası", page.Title);
        Assert.Equal("Sıcak yolluk sistemli kalıplama çözümü.", page.MetaDescription);
        Assert.Equal(["Enjeksiyon Makinası"], page.H1);
        Assert.Contains("Yüksek basınçlı", page.MainText);
    }

    [Fact]
    public async Task Turkish_characters_do_not_inflate_title_length()
    {
        var handler = new StubHttpMessageHandler()
            .MapBytes(PageUri.AbsoluteUri, Windows1254(TurkishHtml), "text/html; Charset=windows-1254");

        var page = await Build(handler).ExtractAsync(PageUri, new PageFetchOptions(BaseUri));

        // Yanlis cozumde her Turkce harf 3 karaktere sisiyor ve META_TITLE_TOO_LONG
        // olmayan bir sorunu bildiriyordu.
        Assert.Equal("Plastik Enjeksiyon Makinası".Length, page.Title!.Length);
    }

    [Fact]
    public async Task Meta_charset_is_used_when_the_header_has_none()
    {
        const string html = """
            <html lang="tr"><head><meta charset="windows-1254"><title>Kalıp</title></head>
            <body><p>Sıcak yolluk</p></body></html>
            """;
        var handler = new StubHttpMessageHandler()
            .MapBytes(PageUri.AbsoluteUri, Windows1254(html), "text/html");

        var page = await Build(handler).ExtractAsync(PageUri, new PageFetchOptions(BaseUri));

        Assert.Equal("Kalıp", page.Title);
    }

    [Fact]
    public async Task Utf8_pages_still_work()
    {
        // Map her zaman UTF-8 kodlar ve charset'i kendisi ekler.
        var handler = new StubHttpMessageHandler().Map(PageUri.AbsoluteUri, TurkishHtml);

        var page = await Build(handler).ExtractAsync(PageUri, new PageFetchOptions(BaseUri));

        Assert.Equal("Plastik Enjeksiyon Makinası", page.Title);
        Assert.Equal(["Enjeksiyon Makinası"], page.H1);
    }

    [Fact]
    public async Task Relative_links_survive_non_utf8_decoding()
    {
        var handler = new StubHttpMessageHandler()
            .MapBytes(PageUri.AbsoluteUri, Windows1254(TurkishHtml), "text/html; Charset=windows-1254");

        var page = await Build(handler).ExtractAsync(PageUri, new PageFetchOptions(BaseUri));

        Assert.Equal("kalıp sayfası", Assert.Single(page.Links).AnchorText);
    }
}
