namespace SeoCopilot.Application.Abstractions;

/// <summary>
/// Bir crawl'in giden istek hizini sinirlar — nezaket butcesi. Sayfa getirmeleri, varlik
/// yoklamalari ve gorsel olcumleri ayni butceyi paylasir; boylece siteye giden toplam hiz
/// istegin turunden bagimsiz olarak sabit kalir.
/// </summary>
public interface IRequestPacer
{
    /// <summary>Bir istek acma izni cikana kadar bekler.</summary>
    ValueTask AcquireAsync(CancellationToken ct = default);
}
