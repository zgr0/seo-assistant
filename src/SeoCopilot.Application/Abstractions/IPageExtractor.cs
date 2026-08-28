namespace SeoCopilot.Application.Abstractions;

/// <summary>Bir URL'i getirip DOM'dan SEO ile ilgili alanlari cikarir. Crawler katmani implemente eder.</summary>
public interface IPageExtractor
{
    Task<ExtractedPage> ExtractAsync(string url, CancellationToken ct = default);
}

/// <summary>Crawler'in bir sayfadan cikardigi ham veri. Kural motoruna girdi olur.</summary>
public record ExtractedPage(
    string Url,
    int StatusCode,
    string? Title,
    string? MetaDescription,
    IReadOnlyList<string> H1,
    IReadOnlyList<string> InternalLinks,
    int WordCount,
    bool HasCanonical);
