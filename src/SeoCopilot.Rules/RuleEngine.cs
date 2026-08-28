using SeoCopilot.Rules.Handlers;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules;

/// <summary>
/// Tum kurallari sirayla calistirir, ihlalleri toplar, skoru hesaplar.
/// Stateless — bir kez kurulup her yerde paylasilabilir.
/// </summary>
public sealed class RuleEngine
{
    private readonly IReadOnlyList<ISeoRule> _rules;

    public RuleEngine(IEnumerable<ISeoRule> rules) => _rules = rules.ToList();

    /// <summary>Varsayilan kural seti ile.</summary>
    public static RuleEngine Default() => new(
    [
        new HttpStatusRule(),
        new TitleMissingRule(),
        new TitleLengthRule(),
        new MetaDescriptionRule(),
        new SingleH1Rule(),
        new CanonicalRule(),
        new ThinContentRule(),
    ]);

    public RuleEvaluation Evaluate(PageInput page)
    {
        var violations = new List<RuleViolation>();
        foreach (var rule in _rules)
        {
            var msg = rule.Evaluate(page);
            if (msg is not null)
                violations.Add(new RuleViolation(rule.Code, rule.Severity, msg));
        }

        return new RuleEvaluation(ScoreCalculator.Calculate(violations), violations);
    }
}
