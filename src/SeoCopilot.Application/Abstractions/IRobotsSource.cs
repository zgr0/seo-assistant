namespace SeoCopilot.Application.Abstractions;

/// <summary>Site kokundeki robots.txt'i getirir. Crawler katmani implemente eder.</summary>
public interface IRobotsSource
{
    Task<IRobotsPolicy> GetAsync(Uri baseUri, CancellationToken ct = default);
}

/// <summary>Ayristirilmis robots.txt politikasi.</summary>
public interface IRobotsPolicy
{
    /// <summary>Bizim user-agent'imiz bu URL'i cekebilir mi.</summary>
    bool IsAllowed(Uri url);

    /// <summary>robots.txt icinde bildirilen sitemap adresleri.</summary>
    IReadOnlyList<string> Sitemaps { get; }
}
