using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Gorsel iceren kucuk bir site: bir blog yazisi (fotograf + logo), fotografsiz bir sayfa ve
/// gorsel dosyalari. Sosyal gonderi gorselinin site fotografindan uretilmesini dener.
/// </summary>
public sealed class ImageTestServer : IAsyncDisposable
{
    private WebApplication _app = null!;

    public string BaseUrl { get; private set; } = string.Empty;

    public static async Task<ImageTestServer> StartAsync(byte[] photo)
    {
        var server = new ImageTestServer();

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app = builder.Build();
        app.MapGet("/{**path}", (string? path) => (path ?? string.Empty).Trim('/') switch
        {
            "" => Html(Home()),
            "blog/fabrika-acilisi" => Html(Article()),
            "sayfa.html" => Html("<html><body>gorsel degil</body></html>"),
            "uploads/fabrika.jpg" => Results.Bytes(photo, "image/jpeg"),
            // Logo gercek bir PNG olsa da adindan elenir.
            "uploads/images/logo.png" => Results.Bytes(photo, "image/png"),
            "robots.txt" => Results.Content("User-agent: *\nAllow: /", "text/plain"),
            _ => Results.NotFound()
        });

        await app.StartAsync();

        server._app = app;
        server.BaseUrl = app.Urls.First().TrimEnd('/');
        return server;
    }

    private static IResult Html(string body) => Results.Content(body, "text/html", Encoding.UTF8);

    private static string Home() => """
        <!doctype html>
        <html lang="tr">
          <head>
            <title>Örnek Makina — enjeksiyon makinaları</title>
            <meta name="description" content="Plastik enjeksiyon makinalarında satış ve teknik servis hizmeti veriyoruz.">
          </head>
          <body>
            <img src="/uploads/images/logo.png" alt="logo">
            <h1>Enjeksiyon makinalarında güvenilir çözüm ortağınız</h1>
            <p>Yirmi yılı aşkın deneyimle plastik, kauçuk ve metal enjeksiyon makinalarının satışını ve servisini yapıyoruz.</p>
            <a href="/blog/fabrika-acilisi">Yeni fabrikamız</a>
          </body>
        </html>
        """;

    private static string Article() => """
        <!doctype html>
        <html lang="tr">
          <head>
            <title>Yeni fabrikamız açıldı</title>
            <meta name="description" content="Yeni üretim tesisimiz kapasitemizi iki katına çıkarıyor.">
          </head>
          <body>
            <img src="/uploads/images/logo.png" alt="logo">
            <h1>Yeni fabrikamız açıldı</h1>
            <img src="/uploads/fabrika.jpg" alt="Yeni fabrika binası">
            <p>Yeni üretim tesisimiz servo motorlu enjeksiyon hatlarıyla kapasitemizi iki katına çıkarıyor ve teslim sürelerini kısaltıyor.</p>
          </body>
        </html>
        """;

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
