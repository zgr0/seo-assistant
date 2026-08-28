using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Rules;

public sealed record RuleViolation(string Code, Severity Severity, string Message);

public sealed record RuleEvaluation(int Score, IReadOnlyList<RuleViolation> Violations);
