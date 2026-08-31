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
        HasCanonical = true,
        ImagesTotal = 2,
        ImagesNoAlt = 0,
        SchemaTypes = ["Article"]
    };

    [Fact]
    public void Healthy_page_scores_100_with_no_violations()
    {
        var result = RuleEngine.Default().Evaluate(HealthyPage());
        Assert.Empty(result.Violations);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Missing_title_triggers_seeded_rule_code()
    {
        var result = RuleEngine.Default().Evaluate(HealthyPage() with { Title = null });
        Assert.Contains(result.Violations, v => v.Code == "META_TITLE_MISSING");
    }

    [Fact]
    public void Multiple_h1_and_missing_h1_are_separate_codes()
    {
        var multiple = RuleEngine.Default().Evaluate(HealthyPage() with { H1 = ["bir", "iki"] });
        Assert.Contains(multiple.Violations, v => v.Code == "H1_MULTIPLE");
        Assert.DoesNotContain(multiple.Violations, v => v.Code == "H1_MISSING");

        var missing = RuleEngine.Default().Evaluate(HealthyPage() with { H1 = [] });
        Assert.Contains(missing.Violations, v => v.Code == "H1_MISSING");
        Assert.DoesNotContain(missing.Violations, v => v.Code == "H1_MULTIPLE");
    }

    [Fact]
    public void Meta_description_missing_and_length_are_separate_codes()
    {
        var missing = RuleEngine.Default().Evaluate(HealthyPage() with { MetaDescription = null });
        Assert.Contains(missing.Violations, v => v.Code == "META_DESCRIPTION_MISSING");
        Assert.DoesNotContain(missing.Violations, v => v.Code == "META_DESCRIPTION_LENGTH");

        var tooShort = RuleEngine.Default().Evaluate(HealthyPage() with { MetaDescription = "kisa" });
        Assert.Contains(tooShort.Violations, v => v.Code == "META_DESCRIPTION_LENGTH");
        Assert.DoesNotContain(tooShort.Violations, v => v.Code == "META_DESCRIPTION_MISSING");
    }

    [Fact]
    public void Noindex_is_flagged()
    {
        var result = RuleEngine.Default().Evaluate(HealthyPage() with { RobotsMeta = "noindex, follow" });
        Assert.Contains(result.Violations, v => v.Code == "NOINDEX_DETECTED");
    }

    [Fact]
    public void Missing_alt_and_schema_are_flagged()
    {
        var result = RuleEngine.Default().Evaluate(
            HealthyPage() with { ImagesNoAlt = 3, SchemaTypes = [] });

        Assert.Contains(result.Violations, v => v.Code == "IMAGE_ALT_MISSING");
        Assert.Contains(result.Violations, v => v.Code == "STRUCTURED_DATA_MISSING");
    }

    [Fact]
    public void Non_2xx_page_reports_only_the_status_rule()
    {
        var result = RuleEngine.Default().Evaluate(
            HealthyPage() with { StatusCode = 404, Title = null, H1 = [], HasCanonical = false });

        Assert.Single(result.Violations);
        Assert.Equal("HTTP_STATUS", result.Violations[0].Code);
    }

    [Fact]
    public void Unreachable_page_is_reported_as_status_failure()
    {
        var result = RuleEngine.Default().Evaluate(HealthyPage() with { StatusCode = 0 });

        Assert.Single(result.Violations);
        Assert.Equal("HTTP_STATUS", result.Violations[0].Code);
    }

    [Fact]
    public void Thin_content_rule_direct()
    {
        var rule = new ThinContentRule();
        Assert.NotNull(rule.Evaluate(HealthyPage() with { WordCount = 50 }));
        Assert.Null(rule.Evaluate(HealthyPage()));
    }

    [Fact]
    public void Every_rule_carries_a_weight_and_category()
    {
        var page = new PageInput { Url = "https://example.com/", StatusCode = 200 };
        var violations = RuleEngine.Default().Evaluate(page).Violations;

        Assert.NotEmpty(violations);
        Assert.All(violations, v => Assert.True(v.Weight > 0));
    }
}
