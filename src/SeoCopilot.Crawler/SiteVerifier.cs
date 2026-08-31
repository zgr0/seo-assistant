using AngleSharp;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Crawler;

/// <summary>Kok sayfada <c>meta[name=seocopilot-verification]</c> etiketini arar.</summary>
public sealed class SiteVerifier(HttpClient http) : ISiteVerifier
{
    public async Task<bool> VerifyMetaTagAsync(string baseUrl, string token, CancellationToken ct = default)
    {
        try
        {
            var html = await http.GetStringAsync(baseUrl, ct);
            var context = BrowsingContext.New(Configuration.Default);
            var doc = await context.OpenAsync(req => req.Content(html), ct);

            var content = doc
                .QuerySelector($"meta[name={SiteService.VerificationMetaName}]")
                ?.GetAttribute("content")
                ?.Trim();

            return string.Equals(content, token, StringComparison.Ordinal);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
