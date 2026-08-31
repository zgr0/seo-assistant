using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler;

/// <summary>
/// Site kokundeki robots.txt'i getirir ve ayristirir. Ayni host icin sonuc onbelleklenir —
/// bir crawl boyunca tek istek yeter. Dosya yoksa/okunamazsa hepsine izin veren politika doner.
/// </summary>
public sealed class RobotsProvider(HttpClient http, IOptions<CrawlerOptions> options) : IRobotsSource
{
    private readonly CrawlerOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, IRobotsPolicy> _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IRobotsPolicy> GetAsync(Uri baseUri, CancellationToken ct = default)
    {
        var key = baseUri.GetLeftPart(UriPartial.Authority);
        if (_cache.TryGetValue(key, out var cached)) return cached;

        var policy = await FetchAsync(new Uri(baseUri, "/robots.txt"), ct);
        _cache[key] = policy;
        return policy;
    }

    private async Task<IRobotsPolicy> FetchAsync(Uri robotsUrl, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(robotsUrl, ct);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                return RobotsTxt.AllowAll();
            if (!response.IsSuccessStatusCode)
                return RobotsTxt.AllowAll();

            var content = await response.Content.ReadAsStringAsync(ct);
            return RobotsTxt.Parse(content, _options.UserAgentToken);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // robots.txt getirilemezse tarama durmaz — kisitsiz kabul edilir.
            return RobotsTxt.AllowAll();
        }
    }
}
