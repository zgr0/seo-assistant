using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Abstractions;

/// <summary>
/// Kural motoruna koprü. SeoCopilot.Rules saf kalsin diye arayuz burada tanimli,
/// adapter Api katmaninda kayitli.
/// </summary>
public interface IRuleRunner
{
    /// <summary>Verilen sayfa icin bulgularin listesini ve 0-100 sayfa skorunu doner.</summary>
    RuleRunOutcome Run(ExtractedPage page);
}

public record RuleRunOutcome(int Score, IReadOnlyList<RuleFinding> Findings);

public record RuleFinding(string RuleCode, Severity Severity, string Message);
