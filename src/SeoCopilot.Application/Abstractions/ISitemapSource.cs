namespace SeoCopilot.Application.Abstractions;

/// <summary>sitemap.xml (ve sitemap index) icinden URL listesi cikarir.</summary>
public interface ISitemapSource
{
    Task<IReadOnlyList<string>> ReadAsync(string sitemapUrl, CancellationToken ct = default);
}
