using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace SeoCopilot.Crawler;

/// <summary>Playwright ile render edilmis sayfanin sonucu.</summary>
public sealed record RenderedPage(
    string Html,
    int StatusCode,
    string? ContentType,
    string? FinalUrl,
    string? XRobotsTag);

/// <summary>
/// Tek bir Chromium instance'i paylasir, her cagriya izole context verir.
/// Uygulama omru boyunca singleton olmali. `playwright install chromium` gerekir.
/// </summary>
/// <param name="options">
/// DI'dan gelir. Testler parametresiz kurabilsin diye istege bagli — o durumda ek
/// Chromium argumani gecilmez.
/// </param>
public sealed class PlaywrightBrowserPool(IOptions<CrawlerOptions>? options = null) : IAsyncDisposable
{
    private readonly string[] _browserArgs = options?.Value.BrowserArgs ?? [];

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is not null) return _browser;
        await _lock.WaitAsync();
        try
        {
            _playwright ??= await Playwright.CreateAsync();
            _browser ??= await _playwright.Chromium.LaunchAsync(new()
            {
                Headless = true,
                Args = _browserArgs
            });
            return _browser;
        }
        finally { _lock.Release(); }
    }

    /// <summary>URL'i render eder; HTML, durum kodu ve SEO icin gereken basliklari doner.</summary>
    public async Task<RenderedPage> RenderAsync(
        Uri url, string userAgent, int timeoutSeconds, CancellationToken ct = default)
    {
        var browser = await GetBrowserAsync();
        await using var context = await browser.NewContextAsync(new() { UserAgent = userAgent });
        var page = await context.NewPageAsync();

        var response = await page.GotoAsync(url.AbsoluteUri, new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = timeoutSeconds * 1000
        });
        ct.ThrowIfCancellationRequested();

        var html = await page.ContentAsync();
        if (response is null)
            return new RenderedPage(html, 0, null, null, null);

        var headers = response.Headers;
        return new RenderedPage(
            html,
            response.Status,
            headers.GetValueOrDefault("content-type"),
            response.Url,
            headers.GetValueOrDefault("x-robots-tag"));
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _lock.Dispose();
    }
}
