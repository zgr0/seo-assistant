using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Rules;

/// <summary>Crawl seviyesi kural girdisi — tek sayfaya bakarak karar verilemeyen kurallar icin.</summary>
public sealed record CrawlPageInput(Guid PageId, string Url, int StatusCode, byte[]? ContentHash);

/// <summary>Hedefi taranmamis linklerde <paramref name="TargetStatusCode"/> null gelir; o link degerlendirilmez.</summary>
public sealed record CrawlLinkInput(Guid FromPageId, string FromUrl, string ToUrl, bool IsInternal, int? TargetStatusCode);

/// <summary>Bir crawl seviyesi bulgu ve baglandigi sayfa (yoksa null → site seviyesi).</summary>
public sealed record CrawlViolation(Guid? PageId, RuleViolation Finding);

/// <summary>
/// Tum crawl'a bakarak calisan kurallar. Saf ve deterministik — sayfa/link kumesinden
/// bulgu uretir, hicbir yan etkisi yoktur.
/// </summary>
public static class CrawlRules
{
    private const int MaxSampleUrls = 10;

    public static IReadOnlyList<CrawlViolation> Evaluate(
        IReadOnlyList<CrawlPageInput> pages,
        IReadOnlyList<CrawlLinkInput> links)
    {
        var result = new List<CrawlViolation>();
        result.AddRange(DuplicateContent(pages));
        result.AddRange(BrokenInternalLinks(links));
        return result;
    }

    /// <summary>DUPLICATE_CONTENT — ayni content_hash birden fazla sayfada.</summary>
    private static IEnumerable<CrawlViolation> DuplicateContent(IReadOnlyList<CrawlPageInput> pages)
    {
        var groups = pages
            .Where(p => p.ContentHash is { Length: > 0 } && p.StatusCode is >= 200 and < 300)
            .GroupBy(p => Convert.ToHexString(p.ContentHash!))
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var samples = members.Select(p => p.Url).Take(MaxSampleUrls).ToList();

            yield return new CrawlViolation(
                members[0].PageId,
                new RuleViolation(
                    "DUPLICATE_CONTENT",
                    RuleCategory.Content,
                    Severity.High,
                    7,
                    $"{members.Count} sayfa ayni icerige sahip.")
                {
                    SampleUrls = samples
                });
        }
    }

    /// <summary>BROKEN_INTERNAL_LINK — 4xx/5xx donen bir sayfaya giden ic link. Kaynak sayfa basina tek bulgu.</summary>
    private static IEnumerable<CrawlViolation> BrokenInternalLinks(IReadOnlyList<CrawlLinkInput> links)
    {
        var broken = links.Where(l =>
            l.IsInternal &&
            l.TargetStatusCode is int code &&
            (code == 0 || code >= 400));

        foreach (var group in broken.GroupBy(l => l.FromPageId))
        {
            var members = group.ToList();
            yield return new CrawlViolation(
                group.Key,
                new RuleViolation(
                    "BROKEN_INTERNAL_LINK",
                    RuleCategory.Links,
                    Severity.High,
                    6,
                    $"{members[0].FromUrl} sayfasinda {members.Count} kirik ic link var.")
                {
                    SampleUrls = [.. members.Select(l => l.ToUrl).Take(MaxSampleUrls)]
                });
        }
    }
}
