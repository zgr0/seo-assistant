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
            WordCount = page.WordCount,
            HasCanonical = page.HasCanonical
        };

        var result = _engine.Evaluate(input);
        var findings = result.Violations
            .Select(v => new RuleFinding(v.Code, v.Severity, v.Message))
            .ToList();

        return new RuleRunOutcome(result.Score, findings);
    }
}
