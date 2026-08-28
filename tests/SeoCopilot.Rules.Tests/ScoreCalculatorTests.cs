using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules;

namespace SeoCopilot.Rules.Tests;

public class ScoreCalculatorTests
{
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
        var violations = new[] { new RuleViolation("X", severity, "msg") };
        Assert.Equal(expected, ScoreCalculator.Calculate(violations));
    }

    [Fact]
    public void Penalties_accumulate()
    {
        var violations = new[]
        {
            new RuleViolation("A", Severity.Critical, "m"), // -25
            new RuleViolation("B", Severity.High, "m"),     // -15
            new RuleViolation("C", Severity.Medium, "m"),   // -8
        };
        Assert.Equal(52, ScoreCalculator.Calculate(violations));
    }

    [Fact]
    public void Score_is_clamped_at_zero()
    {
        var violations = Enumerable.Range(0, 10)
            .Select(i => new RuleViolation($"R{i}", Severity.Critical, "m"))
            .ToArray();
        Assert.Equal(0, ScoreCalculator.Calculate(violations));
    }
}
