using System.Diagnostics;
using SeoCopilot.Application.Common;

namespace SeoCopilot.Crawler.Tests;

/// <summary>
/// Nezaket butcesi: her <c>DelayMs</c> penceresinde <c>Concurrency</c> istek, yani ardisik
/// iki izin arasi <c>DelayMs / Concurrency</c>. Ilk izin beklemesizdir.
/// </summary>
public class RequestPacerTests
{
    private static async Task<long> ElapsedForAsync(RequestPacer pacer, int permits)
    {
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < permits; i++) await pacer.AcquireAsync();
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    [Fact]
    public async Task Zero_delay_never_waits()
    {
        using var pacer = new RequestPacer(concurrency: 1, delayMs: 0);

        Assert.True(await ElapsedForAsync(pacer, 50) < 50);
    }

    [Fact]
    public async Task Permits_are_spaced_by_the_window()
    {
        using var pacer = new RequestPacer(concurrency: 1, delayMs: 60);

        // 3 izin → ilki bedava, sonraki ikisi 60ms'er bekler.
        var elapsed = await ElapsedForAsync(pacer, 3);

        Assert.True(elapsed >= 110, $"beklenen >=110ms, olculen {elapsed}ms");
    }

    [Fact]
    public async Task Concurrency_divides_the_window()
    {
        // 3 istek / 60ms → aralik 20ms.
        using var pacer = new RequestPacer(concurrency: 3, delayMs: 60);

        var elapsed = await ElapsedForAsync(pacer, 4);

        Assert.True(elapsed >= 50, $"beklenen >=50ms, olculen {elapsed}ms");
        Assert.True(elapsed < 160, $"beklenen <160ms, olculen {elapsed}ms");
    }

    [Fact]
    public async Task Parallel_callers_share_one_budget()
    {
        using var pacer = new RequestPacer(concurrency: 1, delayMs: 40);

        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ => await pacer.AcquireAsync()));
        stopwatch.Stop();

        // Es zamanli istekler sirayla gecer — 4 izin, 3 aralik.
        Assert.True(stopwatch.ElapsedMilliseconds >= 110,
            $"beklenen >=110ms, olculen {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Idle_time_is_not_banked()
    {
        using var pacer = new RequestPacer(concurrency: 1, delayMs: 40);

        await pacer.AcquireAsync();
        await Task.Delay(200);

        // Bosta gecen 200ms biriktirilmedi: sonraki izin hazir, ondan sonraki yine bekler.
        var elapsed = await ElapsedForAsync(pacer, 2);

        Assert.True(elapsed >= 30, $"beklenen >=30ms, olculen {elapsed}ms");
        Assert.True(elapsed < 120, $"beklenen <120ms, olculen {elapsed}ms");
    }
}
