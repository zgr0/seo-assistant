using System.Diagnostics;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Application.Common;

/// <summary>
/// Ardisik iki istegin <em>acilisi</em> arasina en az <c>DelayMs / Concurrency</c> koyan
/// nezaket sinirlayicisi. Butce crawl geneli: kim once gelirse sirayi o alir.
///
/// Eski surumde bekleme her is parcaciginin kendi icindeydi (getir → uyu), yani yanit
/// suresi de nezaket borcuna ekleniyor ve isci yuvasi bosa yatiyordu. Burada bekleme
/// isteklerin arasina tasindi: cikan hiz ayni (<c>Concurrency</c> istek her <c>DelayMs</c>),
/// ama yavas bir yanit digerlerini durdurmuyor ve istekler ani obek yerine esit dagiliyor.
///
/// <c>DelayMs = 0</c> → sinir yok, <see cref="AcquireAsync"/> hic beklemez.
/// </summary>
public sealed class RequestPacer : IRequestPacer, IDisposable
{
    private readonly SemaphoreSlim _turn = new(1, 1);
    private readonly long _spacingTicks;

    /// <summary>Bir sonraki istegin acilabilecegi en erken an (Stopwatch zaman damgasi).</summary>
    private long _nextTicks;

    /// <param name="concurrency">Her <paramref name="delayMs"/> penceresinde acilabilecek istek sayisi.</param>
    /// <param name="delayMs">Nezaket penceresi. 0 → sinir yok.</param>
    public RequestPacer(int concurrency, int delayMs)
    {
        if (delayMs <= 0 || concurrency <= 0) return;

        _spacingTicks = (long)(Stopwatch.Frequency * (delayMs / 1000.0) / concurrency);
        _nextTicks = Stopwatch.GetTimestamp();
    }

    public async ValueTask AcquireAsync(CancellationToken ct = default)
    {
        if (_spacingTicks <= 0) return;

        // Sira tek tek verilir: bekleyen kendi yuvasini alirken digerleri kuyrukta durur.
        await _turn.WaitAsync(ct);
        try
        {
            var now = Stopwatch.GetTimestamp();
            if (_nextTicks > now)
                await Task.Delay(Stopwatch.GetElapsedTime(now, _nextTicks), ct);

            // Bosta gecen sure biriktirilmez — uzun bir duraklamadan sonra ani obek olmasin.
            _nextTicks = Math.Max(now, _nextTicks) + _spacingTicks;
        }
        finally
        {
            _turn.Release();
        }
    }

    public void Dispose() => _turn.Dispose();
}
