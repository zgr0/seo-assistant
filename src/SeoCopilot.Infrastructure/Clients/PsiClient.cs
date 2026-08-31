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

/// <summary>
/// Google PageSpeed Insights v5 istemcisi. INP once CrUX alan verisinden okunur
/// (lab olcumu her Lighthouse surumunde bulunmuyor), yoksa lab denetimine dusulur.
/// </summary>
public sealed class PsiClient(HttpClient http, IOptions<PsiOptions> options) : IPageSpeedClient
{
    private readonly PsiOptions _opt = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_opt.ApiKey);

    public async Task<PageSpeedResult> AnalyzeAsync(string url, CancellationToken ct = default)
    {
        var endpoint = $"https://www.googleapis.com/pagespeedonline/v5/runPagespeed?url={Uri.EscapeDataString(url)}&strategy=mobile";
        if (!string.IsNullOrEmpty(_opt.ApiKey))
            endpoint += $"&key={_opt.ApiKey}";

        var doc = await http.GetFromJsonAsync<JsonElement>(endpoint, ct);

        var audits = doc.TryGetProperty("lighthouseResult", out var lighthouse)
            && lighthouse.TryGetProperty("audits", out var a)
            ? a
            : default;

        return new PageSpeedResult(
            Performance: PerformanceScore(lighthouse),
            LargestContentfulPaintMs: Audit(audits, "largest-contentful-paint"),
            CumulativeLayoutShift: Audit(audits, "cumulative-layout-shift"),
            InteractionToNextPaintMs: FieldMetric(doc, "INTERACTION_TO_NEXT_PAINT")
                ?? Audit(audits, "interaction-to-next-paint"),
            TimeToFirstByteMs: Audit(audits, "server-response-time"),
            FirstContentfulPaintMs: Audit(audits, "first-contentful-paint"));
    }

    private static int? PerformanceScore(JsonElement lighthouse) =>
        lighthouse.ValueKind == JsonValueKind.Object
        && lighthouse.TryGetProperty("categories", out var categories)
        && categories.TryGetProperty("performance", out var performance)
        && performance.TryGetProperty("score", out var score)
        && score.ValueKind == JsonValueKind.Number
            ? (int)Math.Round(score.GetDouble() * 100)
            : null;

    private static double? Audit(JsonElement audits, string id) =>
        audits.ValueKind == JsonValueKind.Object
        && audits.TryGetProperty(id, out var audit)
        && audit.TryGetProperty("numericValue", out var value)
        && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;

    /// <summary>CrUX alan verisi — 75. persentil.</summary>
    private static double? FieldMetric(JsonElement doc, string metric) =>
        doc.TryGetProperty("loadingExperience", out var experience)
        && experience.TryGetProperty("metrics", out var metrics)
        && metrics.TryGetProperty(metric, out var entry)
        && entry.TryGetProperty("percentile", out var percentile)
        && percentile.ValueKind == JsonValueKind.Number
            ? percentile.GetDouble()
            : null;
}
