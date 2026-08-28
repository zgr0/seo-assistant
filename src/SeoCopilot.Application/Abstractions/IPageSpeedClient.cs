namespace SeoCopilot.Application.Abstractions;

/// <summary>Google PageSpeed Insights (PSI) istemcisi.</summary>
public interface IPageSpeedClient
{
    Task<PageSpeedResult> AnalyzeAsync(string url, CancellationToken ct = default);
}

/// <summary>PSI'dan donen ozet metrikler (0-100 arasi performans + Core Web Vitals ms).</summary>
public record PageSpeedResult(int Performance, double LargestContentfulPaintMs, double CumulativeLayoutShift);
