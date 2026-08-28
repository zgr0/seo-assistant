using SeoCopilot.Rules;
using SeoCopilot.Rules.Handlers;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Tests;

public class RuleEngineTests
{
    private static PageInput HealthyPage() => new()
    {
        Url = "https://example.com/",
        StatusCode = 200,
        Title = "Ornek sayfa — kaliteli ve dogru uzunlukta baslik metni",
        MetaDescription = new string('a', 120),
        H1 = ["Tek baslik"],
        InternalLinks = ["/hakkinda"],
        WordCount = 500,
        HasCanonical = true
    };

    [Fact]
    public void Healthy_page_scores_100_with_no_violations()
    {
        var result = RuleEngine.Default().Evaluate(HealthyPage());
        Assert.Empty(result.Violations);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Missing_title_triggers_critical_rule()
    {
        var page = HealthyPage() with { Title = null };
        var result = RuleEngine.Default().Evaluate(page);
        Assert.Contains(result.Violations, v => v.Code == "TITLE_MISSING");
    }

    [Fact]
    public void Multiple_h1_flagged()
    {
        var page = HealthyPage() with { H1 = ["bir", "iki"] };
        var result = RuleEngine.Default().Evaluate(page);
        Assert.Contains(result.Violations, v => v.Code == "H1_COUNT");
    }

    [Fact]
    public void Thin_content_rule_direct()
    {
        var rule = new ThinContentRule();
        Assert.NotNull(rule.Evaluate(HealthyPage() with { WordCount = 50 }));
        Assert.Null(rule.Evaluate(HealthyPage()));
    }
}
