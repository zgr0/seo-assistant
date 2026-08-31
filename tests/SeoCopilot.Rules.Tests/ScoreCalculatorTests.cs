using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules;

namespace SeoCopilot.Rules.Tests;

public class ScoreCalculatorTests
{
    private static RuleViolation V(Severity severity, RuleCategory category = RuleCategory.Meta) =>
        new("X", category, severity, 1, "msg");

    [Fact]
    public void No_violations_is_100()
    {
        Assert.Equal(100, ScoreCalculator.Calculate([]));
    }

    [Theory]
    [InlineData(Severity.Critical, 75)]
    [InlineData(Severity.High, 85)]
    [InlineData(Severity.Medium, 92)]
    [InlineData(Severity.Low, 97)]
    [InlineData(Severity.Info, 100)]
    public void Single_violation_applies_severity_penalty(Severity severity, int expected)
    {
        Assert.Equal(expected, ScoreCalculator.Calculate([V(severity)]));
    }

    [Fact]
    public void Penalties_accumulate()
    {
        RuleViolation[] violations =
        [
            V(Severity.Critical), // -25
            V(Severity.High),     // -15
            V(Severity.Medium),   // -8
        ];
        Assert.Equal(52, ScoreCalculator.Calculate(violations));
    }

    [Fact]
    public void Score_is_clamped_at_zero()
    {
        var violations = Enumerable.Range(0, 10).Select(_ => V(Severity.Critical)).ToArray();
        Assert.Equal(0, ScoreCalculator.Calculate(violations));
    }

    [Fact]
    public void Overall_is_average_of_page_scores()
    {
        Assert.Equal(80m, ScoreCalculator.CalculateOverall([100, 60], []));
    }

    [Fact]
    public void Overall_subtracts_crawl_level_penalties()
    {
        // ortalama 90, crawl seviyesi bir High bulgu -15
        Assert.Equal(75m, ScoreCalculator.CalculateOverall([100, 80], [V(Severity.High)]));
    }

    [Fact]
    public void Overall_without_pages_is_zero()
    {
        Assert.Equal(0m, ScoreCalculator.CalculateOverall([], []));
    }

    [Fact]
    public void Category_scores_spread_penalty_across_pages()
    {
        // 2 sayfa, meta kategorisinde tek Medium ihlal (-8) → 100 - 8/2 = 96
        var scores = ScoreCalculator.CalculateCategoryScores([V(Severity.Medium)], pageCount: 2);

        Assert.Equal(96m, scores["meta"]);
        Assert.Single(scores);
    }

    [Fact]
    public void Category_scores_are_keyed_by_snake_case()
    {
        var scores = ScoreCalculator.CalculateCategoryScores(
            [V(Severity.Low, RuleCategory.StructuredData)], pageCount: 1);

        Assert.True(scores.ContainsKey("structured_data"));
    }

    [Fact]
    public void Snapshot_carries_the_penalty_table()
    {
        var snapshot = ScoreCalculator.Snapshot();

        Assert.Equal(25, snapshot["critical"]);
        Assert.Equal(15, snapshot["high"]);
        Assert.Equal(8, snapshot["medium"]);
        Assert.Equal(3, snapshot["low"]);
        Assert.Equal(0, snapshot["info"]);
    }
}
