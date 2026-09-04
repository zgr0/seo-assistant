using System.Collections.Concurrent;
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

    /// <summary>
    /// Gorsel boyutu onbellegi — ayni logo her sayfada yeniden sorulmasin. Deger olcumun
    /// kendisi degil <see cref="Task{TResult}"/>'i: ayni adres iki sayfada es zamanli gecerse
    /// ikinci istek acilmaz, birincinin sonucu beklenir. Sonuc null = olculemedi.
    /// </summary>
    private readonly ConcurrentDictionary<string, Task<long?>> _imageSizes = new(StringComparer.Ordinal);

    public async Task<ExtractedPage> ExtractAsync(Uri url, PageFetchOptions fetch, CancellationToken ct = default)
    {
        var page = fetch.RenderJs
            ? await ExtractRenderedAsync(url, fetch, ct)
            : await ExtractHttpAsync(url, fetch, ct);

        return page with { ImageSizes = await MeasureImagesAsync(page.ImageUrls, fetch, ct) };
    }

    /// <summary>
    /// HEAD ile durum yoklar. Bazi sunucular HEAD'e 405/501 doner — o durumda govde
    /// okunmadan GET denenir (ResponseHeadersRead).
    /// </summary>
    public async Task<ExtractedPage> ProbeAsync(Uri url, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var (status, contentType, redirectTo) = await ProbeCoreAsync(url, HttpMethod.Head, ct);

            if (status is (int)HttpStatusCode.MethodNotAllowed or (int)HttpStatusCode.NotImplemented)
                (status, contentType, redirectTo) = await ProbeCoreAsync(url, HttpMethod.Get, ct);

            stopwatch.Stop();
            return new ExtractedPage
            {
                Url = url.AbsoluteUri,
                StatusCode = status,
                ContentType = contentType,
                RedirectTo = redirectTo,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            return ExtractedPage.Failed(url, (int)stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<(int Status, string? ContentType, string? RedirectTo)> ProbeCoreAsync(
        Uri url, HttpMethod method, CancellationToken ct)
    {
        var current = url;
        string? redirectTo = null;
        HttpResponseMessage? response = null;

        try
        {
            for (var hop = 0; ; hop++)
            {
                response?.Dispose();
                using var request = new HttpRequestMessage(method, current);
                response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

                if (!IsRedirect(response.StatusCode) || hop >= _options.MaxRedirects) break;

                var location = response.Headers.Location;
                if (location is null) break;

                // Sayfa getirmedeki ile ayni kural: http disi hedef izlenmez.
                var next = new Uri(current, location);
                if (!IsHttp(next)) break;

                current = next;
                redirectTo = current.AbsoluteUri;
            }

            return ((int)response.StatusCode, response.Content.Headers.ContentType?.ToString(), redirectTo);
        }
        finally
        {
            response?.Dispose();
        }
    }

    private async Task<ExtractedPage> ExtractHttpAsync(Uri url, PageFetchOptions fetch, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        var current = url;
        string? redirectTo = null;
        string? invalidRedirect = null;
        var redirects = 0;
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

                var next = new Uri(current, location);

                // http/https disi hedef (orn. `Location: javascript:;`) izlenemez. Istek
                // denenirse istisna cikar ve sayfa "getirilemedi" gorunur — oysa sunucu
                // duzgun yanit verdi, hatali olan yonlendirmenin kendisi.
                if (!IsHttp(next))
                {
                    invalidRedirect = location.OriginalString;
                    break;
                }

                current = next;
                redirectTo = current.AbsoluteUri;
                redirects++;
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
                    RedirectCount = redirects,
                    InvalidRedirectTarget = invalidRedirect,
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    RobotsMeta = xRobots
                };
            }

            var body = await ReadCappedAsync(response, ct);
            stopwatch.Stop();

            // Govde ham bayt olarak gecer; charset'i contentType tasiyor.
            // Goreli adresler zincirin sonundaki belgeye gore cozulur — `current`, `url` degil.
            var page = await _analyzer.AnalyzeAsync(
                body, contentType, url, current, fetch.BaseUri, status, contentType, redirectTo,
                (int)stopwatch.ElapsedMilliseconds, body.Length, xRobots, ct);

            return page with { RedirectCount = redirects };
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

        // Playwright yonlendirmeleri kendi izler; goreli adreslerin tabani varilan adrestir.
        var documentUrl = Uri.TryCreate(rendered.FinalUrl, UriKind.Absolute, out var final) ? final : url;

        // Playwright govdeyi zaten cozmus string olarak veriyor. AngleSharp'a bayt olarak
        // gecerken charset'i acikca utf-8 diyoruz — yoksa sayfanin <meta charset>'ini
        // (orn. windows-1254) gorup UTF-8 baytlari yanlis cozer.
        var body = Encoding.UTF8.GetBytes(rendered.Html);
        var size = body.Length;

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
            body, "text/html; charset=utf-8",
            url, documentUrl, fetch.BaseUri, rendered.StatusCode, rendered.ContentType, redirectTo,
            (int)stopwatch.ElapsedMilliseconds, size, rendered.XRobotsTag, ct);
    }

    /// <summary>
    /// Gorsellerin indirme boyutunu HEAD ile olcer (Content-Length). Sayfa basina
    /// <see cref="CrawlerOptions.MaxImageChecksPerPage"/> gorsele bakilir; olculemeyenler atlanir.
    ///
    /// Bunlar sayfa disi ek isteklerdir; bu yuzden robots.txt'ye uyar. robots yalniz sitenin
    /// kendi authority'sindeki gorsellere uygulanir — CDN'in robots'unu bilmiyoruz.
    ///
    /// Istekler acilir acilmaz beklenmez: once hepsi baslatilir, sonra sonuclari toplanir —
    /// boylece yanit sureleri ust uste biner. Acilislar crawl'in nezaket butcesinden
    /// (<see cref="PageFetchOptions.Pacer"/>) izin alir; sayfa getirmeleriyle ayni butce.
    /// Onbellekten donen olcum ne istek ne izin tuketir.
    /// </summary>
    private async Task<IReadOnlyList<MeasuredImage>> MeasureImagesAsync(
        IReadOnlyList<string> imageUrls, PageFetchOptions fetch, CancellationToken ct)
    {
        if (_options.MaxImageChecksPerPage <= 0 || imageUrls.Count == 0) return [];

        var pending = new List<(string Url, Task<long?> Size)>();

        foreach (var imageUrl in imageUrls)
        {
            // Butce onbellek isabetlerini de sayar — sayfa basina bakilan gorsel sayisi sabit.
            if (pending.Count >= _options.MaxImageChecksPerPage) break;
            if (!IsAllowed(imageUrl, fetch)) continue;

            if (_imageSizes.TryGetValue(imageUrl, out var cached))
            {
                pending.Add((imageUrl, cached));
                continue;
            }

            // Yeni olcum → nezaket izni. Iki is parcacigi ayni anda kacirirsa biri izni bosa
            // harcar; hiz eksige degil fazlaya kacmaz, kabul edilebilir.
            if (fetch.Pacer is not null) await fetch.Pacer.AcquireAsync(ct);

            // Olcum gorevi crawl geneli paylasilir; tek sayfanin token'ina baglanamaz, yoksa
            // o sayfa iptal olunca ayni gorseli bekleyen digerleri de duser. Sinir: HttpClient.Timeout.
            pending.Add((imageUrl, _imageSizes.GetOrAdd(imageUrl, HeadContentLengthAsync)));
        }

        var measured = new List<MeasuredImage>(pending.Count);
        foreach (var (url, size) in pending)
        {
            if (await size.WaitAsync(ct) is long bytes) measured.Add(new MeasuredImage(url, bytes));
        }

        return measured;
    }

    /// <summary>Sitenin kendi authority'sindeki adresler robots.txt ile sinirlanir.</summary>
    private static bool IsAllowed(string imageUrl, PageFetchOptions fetch)
    {
        if (fetch.Robots is null) return true;
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var url)) return false;

        var sameAuthority = string.Equals(
            url.GetLeftPart(UriPartial.Authority),
            fetch.BaseUri.GetLeftPart(UriPartial.Authority),
            StringComparison.OrdinalIgnoreCase);

        return !sameAuthority || fetch.Robots.IsAllowed(url);
    }

    /// <summary>
    /// Tek gorselin boyutu. Sonucu birden cok sayfa paylastigi icin token almaz —
    /// suresini <see cref="HttpClient.Timeout"/> sinirlar. Hicbir kosulda firlatmaz:
    /// onbellege hatali gorev yazilmasin.
    /// </summary>
    private async Task<long?> HeadContentLengthAsync(string imageUrl)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, imageUrl);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            return response.IsSuccessStatusCode ? response.Content.Headers.ContentLength : null;
        }
        catch (Exception)
        {
            // Olculemeyen gorsel bulgu uretmez — crawl'i da dusurmez.
            return null;
        }
    }

    /// <summary>
    /// Govdeyi <see cref="CrawlerOptions.MaxHtmlBytes"/> ile sinirli okur. Cozme yapilmaz —
    /// karakter kodlamasini <see cref="HtmlAnalyzer"/> icinde AngleSharp secer.
    /// </summary>
    private async Task<byte[]> ReadCappedAsync(HttpResponseMessage response, CancellationToken ct)
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

        return buffer.ToArray();
    }

    private static bool IsRedirect(HttpStatusCode status) => (int)status is >= 300 and < 400;

    private static bool IsHttp(Uri url) =>
        url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps;

    /// <summary>Yalniz 2xx + text/html ayristirilir; hata sayfalarindan link toplanmaz.</summary>
    private static bool IsParsableHtml(int status, string? contentType) =>
        status is >= 200 and < 300
        && contentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true;
}
