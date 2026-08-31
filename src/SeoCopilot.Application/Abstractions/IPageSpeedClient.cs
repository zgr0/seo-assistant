namespace SeoCopilot.Application.Abstractions;

/// <summary>Google PageSpeed Insights (PSI) istemcisi.</summary>
public interface IPageSpeedClient
{
    /// <summary>API anahtari tanimli mi — degilse crawl PSI adimini atlar.</summary>
    bool IsConfigured { get; }

    Task<PageSpeedResult> AnalyzeAsync(string url, CancellationToken ct = default);
}

/// <summary>
/// PSI'dan donen ozet metrikler. Lighthouse ya da CrUX yanitinda bulunmayan
/// her metrik null gelir; kural motoru null metrigi degerlendirmez.
/// </summary>
public record PageSpeedResult(
    int? Performance,
    double? LargestContentfulPaintMs,
    double? CumulativeLayoutShift,
    double? InteractionToNextPaintMs = null,
    double? TimeToFirstByteMs = null,
    double? FirstContentfulPaintMs = null);
