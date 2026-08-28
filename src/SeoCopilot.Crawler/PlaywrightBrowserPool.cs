using Microsoft.Playwright;

namespace SeoCopilot.Crawler;

/// <summary>
/// Tek bir Chromium instance'i paylasir, her cagriya izole context verir.
/// Uygulama omru boyunca singleton olmali. `playwright install chromium` gerekir.
/// </summary>
public sealed class PlaywrightBrowserPool : IAsyncDisposable
{
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
            _browser ??= await _playwright.Chromium.LaunchAsync(new() { Headless = true });
            return _browser;
        }
        finally { _lock.Release(); }
    }

    /// <summary>URL'i render eder, tam HTML doner.</summary>
    public async Task<string> RenderHtmlAsync(string url, CancellationToken ct = default)
    {
        var browser = await GetBrowserAsync();
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        ct.ThrowIfCancellationRequested();
        return await page.ContentAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _lock.Dispose();
    }
}
