using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Crawler'i gercek HTTP uzerinden denemek icin rastgele portta ayaga kalkan kucuk statik site.
/// Yapisi kasitli: yinelenen icerik, kirik ic link, noindex sayfa, robots ile engellenmis yol ve
/// yalniz sitemap'ten ulasilabilen bir sayfa icerir.
/// </summary>
public sealed class TestWebSite : IAsyncDisposable
{
    private WebApplication _app = null!;

    public string BaseUrl { get; private set; } = string.Empty;

    public static async Task<TestWebSite> StartAsync()
    {
        var site = new TestWebSite();

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app = builder.Build();
        app.MapGet("/{**path}", (HttpContext ctx, string? path) =>
            site.Serve(path ?? string.Empty, $"{ctx.Request.Scheme}://{ctx.Request.Host}"));

        await app.StartAsync();

        site._app = app;
        site.BaseUrl = app.Urls.First().TrimEnd('/');
        return site;
    }

    private IResult Serve(string path, string origin) => path.Trim('/') switch
    {
        "" => Html(Home()),
        "a" or "b" => Html(Duplicate()),
        "c" => Html(NoIndex()),
        "sitemap-only" => Html(Simple("Yalniz sitemap'te olan sayfa", "Bu sayfaya hicbir sayfadan link yok.")),
        "gizli/x" => Html(Simple("Gizli sayfa", "robots ile engellendi.")),
        "kirik" => Results.Content(Simple("Bulunamadi", "yok"), "text/html", Encoding.UTF8, 404),
        "robots.txt" => Results.Content(Robots(origin), "text/plain"),
        "sitemap.xml" => Results.Content(Sitemap(origin), "application/xml"),
        _ => Results.Content("<html><body>yok</body></html>", "text/html", Encoding.UTF8, 404)
    };

    private static IResult Html(string body) => Results.Content(body, "text/html", Encoding.UTF8);

    private static string Robots(string origin) => $"""
        User-agent: *
        Disallow: /gizli
        Sitemap: {origin}/sitemap.xml
        """;

    private static string Sitemap(string origin) => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
          <url><loc>{origin}/</loc></url>
          <url><loc>{origin}/sitemap-only</loc></url>
        </urlset>
        """;

    private static string Home()
    {
        return $$"""
            <!doctype html>
            <html lang="tr">
              <head>
                <title>Ana sayfa — test sitesi icin yeterince uzun bir baslik</title>
                <meta name="description" content="Bu ana sayfanin aciklamasi; uzunlugu SEO icin onerilen araliga denk gelsin diye yeterince uzun tutulmustur.">
                <link rel="canonical" href="/">
                <script type="application/ld+json">{"@type":"WebSite"}</script>
              </head>
              <body>
                <h1>Ana sayfa</h1>
                <p>Test sitesinin ana sayfasi.</p>
                <h2>Bolum</h2>
                <img src="/logo.png" alt="logo">
                <a href="/a">birinci sayfa</a>
                <a href="/b">ikinci sayfa</a>
                <a href="/kirik">kirik link</a>
                <a href="/gizli/x">gizli alan</a>
                <a href="https://disari.example/x" rel="nofollow">dis site</a>
              </body>
            </html>
            """;
    }

    /// <summary>/a ve /b bilerek birebir ayni — DUPLICATE_CONTENT tetiklenmeli.</summary>
    private static string Duplicate() => """
        <!doctype html>
        <html lang="tr">
          <head>
            <title>Ayni icerik — yinelenen sayfa basligi burada</title>
            <meta name="description" content="Iki farkli URL'de birebir ayni govde metni bulunuyor; yinelenen icerik kurali bunu yakalamali.">
            <link rel="canonical" href="/a">
            <script type="application/ld+json">{"@type":"Article"}</script>
          </head>
          <body>
            <h1>Yinelenen sayfa</h1>
            <p>Bu iki sayfanin govdesi birebir ayni.</p>
            <img src="/x.png" alt="gorsel">
            <a href="/c">ucuncu sayfa</a>
          </body>
        </html>
        """;

    private static string NoIndex() => """
        <!doctype html>
        <html lang="tr">
          <head>
            <title>Dizine kapali sayfa — yeterince uzun bir baslik</title>
            <meta name="description" content="Bu sayfa noindex tasiyor; NOINDEX_DETECTED kuralinin tetiklenmesi bekleniyor.">
            <meta name="robots" content="noindex, follow">
            <link rel="canonical" href="/c">
            <script type="application/ld+json">{"@type":"WebPage"}</script>
          </head>
          <body>
            <h1>Dizine kapali</h1>
            <p>Bu sayfa arama sonuclarinda gorunmemeli.</p>
          </body>
        </html>
        """;

    private static string Simple(string title, string body) => $"""
        <!doctype html>
        <html lang="tr">
          <head><title>{title}</title></head>
          <body><h1>{title}</h1><p>{body}</p></body>
        </html>
        """;

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
