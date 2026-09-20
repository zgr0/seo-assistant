using Sitecraft.Domain.Enums;

namespace Sitecraft.Rules;

public sealed record RuleViolation(
    string Code,
    RuleCategory Category,
    Severity Severity,
    int Weight,
    string Message)
{
    /// <summary>Crawl seviyesi kurallarda ornek URL'ler; sayfa kurallarinda bos.</summary>
    public IReadOnlyList<string> SampleUrls { get; init; } = [];
}

public sealed record RuleEvaluation(int Score, IReadOnlyList<RuleViolation> Violations);
