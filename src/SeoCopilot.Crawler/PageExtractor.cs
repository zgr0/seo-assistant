using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler;

/// <summary>
/// Sayfayi getirir ve <see cref="HtmlAnalyzer"/> ile ayristirir.
/// RenderJs kapaliysa HttpClient, aciksa Playwright (JS calistirilir).
/// Yonlendirmeler elle izlenir: durum kodu zincirin sonundaki yanittan, RedirectTo ise
/// yonlendirme olduysa varilan adresten gelir — boylece http→https gibi zincirler
/// HTTP_STATUS kuralini tetiklemez ama kayitta gorunur.
/// </summary>
public sealed class PageExtractor(
    HttpClient http,
    PlaywrightBrowserPool browsers,
    IOptions<CrawlerOptions> options) : IPageExtractor
{
    private readonly CrawlerOptions _options = options.Value;
    private readonly HtmlAnalyzer _analyzer = new(options.Value.MaxMainTextChars);

    public Task<ExtractedPage> ExtractAsync(Uri url, PageFetchOptions fetch, CancellationToken ct = default) =>
        fetch.RenderJs
            ? ExtractRenderedAsync(url, fetch, ct)
            : ExtractHttpAsync(url, fetch, ct);

    private async Task<ExtractedPage> ExtractHttpAsync(Uri url, PageFetchOptions fetch, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        var current = url;
        string? redirectTo = null;
        HttpResponseMessage? response = null;

        try
        {
            for (var hop = 0; ; hop++)
            {
                response?.Dispose();
                response = await http.GetAsync(current, HttpCompletionOption.ResponseHeadersRead, ct);

                if (!IsRedirect(response.StatusCode) || hop >= _options.MaxRedirects) break;

                var location = response.Headers.Location;
                if (location is null) break;

                current = new Uri(current, location);
                redirectTo = current.AbsoluteUri;
            }

            var status = (int)response.StatusCode;
            var contentType = response.Content.Headers.ContentType?.ToString();
            var xRobots = response.Headers.TryGetValues("X-Robots-Tag", out var values)
                ? string.Join(", ", values)
                : null;

            if (!IsParsableHtml(status, contentType))
            {
                stopwatch.Stop();
                return new ExtractedPage
                {
                    Url = url.AbsoluteUri,
                    StatusCode = status,
                    ContentType = contentType,
                    RedirectTo = redirectTo,
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    RobotsMeta = xRobots
                };
            }

            var (html, size) = await ReadCappedAsync(response, ct);
            stopwatch.Stop();

            return await _analyzer.AnalyzeAsync(
                html, url, fetch.BaseUri, status, contentType, redirectTo,
                (int)stopwatch.ElapsedMilliseconds, size, xRobots, ct);
        }
        finally
        {
            response?.Dispose();
        }
    }

    private async Task<ExtractedPage> ExtractRenderedAsync(Uri url, PageFetchOptions fetch, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var rendered = await browsers.RenderAsync(url, _options.UserAgent, _options.RequestTimeoutSeconds, ct);
        stopwatch.Stop();

        var redirectTo = rendered.FinalUrl is not null && rendered.FinalUrl != url.AbsoluteUri
            ? rendered.FinalUrl
            : null;
        var size = Encoding.UTF8.GetByteCount(rendered.Html);

        if (!IsParsableHtml(rendered.StatusCode, rendered.ContentType))
        {
            return new ExtractedPage
            {
                Url = url.AbsoluteUri,
                StatusCode = rendered.StatusCode,
                ContentType = rendered.ContentType,
                RedirectTo = redirectTo,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                HtmlSizeBytes = size,
                RobotsMeta = rendered.XRobotsTag
            };
        }

        return await _analyzer.AnalyzeAsync(
            rendered.Html, url, fetch.BaseUri, rendered.StatusCode, rendered.ContentType, redirectTo,
            (int)stopwatch.ElapsedMilliseconds, size, rendered.XRobotsTag, ct);
    }

    /// <summary>Govdeyi <see cref="CrawlerOptions.MaxHtmlBytes"/> ile sinirli okur.</summary>
    private async Task<(string Html, int Bytes)> ReadCappedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();

        var chunk = new byte[8192];
        int read;
        while (buffer.Length < _options.MaxHtmlBytes
            && (read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            var allowed = (int)Math.Min(read, _options.MaxHtmlBytes - buffer.Length);
            buffer.Write(chunk, 0, allowed);
        }

        var bytes = buffer.ToArray();
        return (Encoding.UTF8.GetString(bytes), bytes.Length);
    }

    private static bool IsRedirect(HttpStatusCode status) => (int)status is >= 300 and < 400;

    /// <summary>Yalniz 2xx + text/html ayristirilir; hata sayfalarindan link toplanmaz.</summary>
    private static bool IsParsableHtml(int status, string? contentType) =>
        status is >= 200 and < 300
        && contentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true;
}
