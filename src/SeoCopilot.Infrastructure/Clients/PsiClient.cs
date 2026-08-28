using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Infrastructure.Clients;

public sealed class PsiOptions
{
    public const string Section = "PageSpeed";
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>Google PageSpeed Insights v5 istemcisi.</summary>
public sealed class PsiClient(HttpClient http, IOptions<PsiOptions> options) : IPageSpeedClient
{
    private readonly PsiOptions _opt = options.Value;

    public async Task<PageSpeedResult> AnalyzeAsync(string url, CancellationToken ct = default)
    {
        var endpoint = $"https://www.googleapis.com/pagespeedonline/v5/runPagespeed?url={Uri.EscapeDataString(url)}&strategy=mobile";
        if (!string.IsNullOrEmpty(_opt.ApiKey))
            endpoint += $"&key={_opt.ApiKey}";

        var doc = await http.GetFromJsonAsync<JsonElement>(endpoint, ct);

        var lighthouse = doc.GetProperty("lighthouseResult");
        var audits = lighthouse.GetProperty("audits");

        var perf = (int)Math.Round(
            lighthouse.GetProperty("categories").GetProperty("performance").GetProperty("score").GetDouble() * 100);
        var lcp = audits.GetProperty("largest-contentful-paint").GetProperty("numericValue").GetDouble();
        var cls = audits.GetProperty("cumulative-layout-shift").GetProperty("numericValue").GetDouble();

        return new PageSpeedResult(perf, lcp, cls);
    }
}
