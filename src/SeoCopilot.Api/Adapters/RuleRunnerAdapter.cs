using SeoCopilot.Application.Abstractions;
using SeoCopilot.Rules;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Api.Adapters;

/// <summary>
/// Application ile saf SeoCopilot.Rules arasindaki koprü.
/// Rules projesi Application'i tanimadigindan eslesme burada.
/// </summary>
public sealed class RuleRunnerAdapter : IRuleRunner
{
    private readonly RuleEngine _engine = RuleEngine.Default();

    public RuleRunOutcome Run(ExtractedPage page)
    {
        var input = new PageInput
        {
            Url = page.Url,
            StatusCode = page.StatusCode,
            Title = page.Title,
            MetaDescription = page.MetaDescription,
            H1 = page.H1,
            InternalLinks = page.InternalLinks,
            InternalAnchorTexts = page.InternalAnchorTexts,
            WordCount = page.WordCount,
            HasCanonical = page.HasCanonical,
            CanonicalUrl = page.CanonicalUrl,
            RedirectCount = page.RedirectCount,
            RobotsMeta = page.RobotsMeta,
            HeadingLevels = page.HeadingLevels,
            ImagesTotal = page.ImagesTotal,
            ImagesNoAlt = page.ImagesNoAlt,
            ImageSizes = [.. page.ImageSizes.Select(i => new ImageSize(i.Url, i.Bytes))],
            SchemaTypes = page.SchemaTypes,
            OgTags = page.OgTags,
            Lang = page.Lang
        };

        var result = _engine.Evaluate(input);
        return new RuleRunOutcome(result.Score, [.. result.Violations.Select(ToFinding)]);
    }

    public IReadOnlyList<CrawlRuleFinding> RunCrawl(
        IReadOnlyList<CrawlPageFacts> pages,
        IReadOnlyList<CrawlLinkFacts> links,
        CrawlSiteFacts site)
    {
        var pageInputs = pages
            .Select(p => new CrawlPageInput(p.PageId, p.Url, p.StatusCode, p.ContentHash)
            {
                Depth = p.Depth,
                InlinkCount = p.InlinkCount,
                Title = p.Title,
                MetaDescription = p.MetaDescription,
                IsHome = p.IsHome,
                NoIndex = p.NoIndex
            })
            .ToList();

        var linkInputs = links
            .Select(l => new CrawlLinkInput(l.FromPageId, l.FromUrl, l.ToUrl, l.IsInternal, l.TargetStatusCode))
            .ToList();

        var siteInput = new CrawlSiteInput
        {
            SitemapFound = site.SitemapFound,
            SitemapUrls = site.SitemapUrls,
            BlockedUrls = site.BlockedUrls,
            HomePageId = site.HomePageId,
            Vitals = site.Vitals is { } v ? new VitalsInput(v.LcpMs, v.Cls, v.InpMs) : null
        };

        return [.. CrawlRules.Evaluate(pageInputs, linkInputs, siteInput)
            .Select(v => new CrawlRuleFinding(v.PageId, ToFinding(v.Finding)))];
    }

    public decimal OverallScore(IEnumerable<int> pageScores, IEnumerable<RuleFinding> crawlFindings) =>
        ScoreCalculator.CalculateOverall(pageScores, crawlFindings.Select(ToViolation));

    public Dictionary<string, decimal> CategoryScores(IEnumerable<RuleFinding> allFindings, int pageCount) =>
        ScoreCalculator.CalculateCategoryScores(allFindings.Select(ToViolation), pageCount);

    public Dictionary<string, int> ScoringSnapshot() => ScoreCalculator.Snapshot();

    private static RuleFinding ToFinding(RuleViolation v) =>
        new(v.Code, v.Category, v.Severity, v.Weight, v.Message) { SampleUrls = v.SampleUrls };

    private static RuleViolation ToViolation(RuleFinding f) =>
        new(f.RuleCode, f.Category, f.Severity, f.Weight, f.Message) { SampleUrls = f.SampleUrls };
}
