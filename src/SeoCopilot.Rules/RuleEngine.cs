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

    public RuleEngine(IEnumerable<ISeoRule> rules) => _rules = [.. rules];

    /// <summary>Varsayilan kural seti — kodlar rules tablosu seed'i ile birebir ayni.</summary>
    public static RuleEngine Default() => new(
    [
        new HttpStatusRule(),
        new NoIndexRule(),
        new MetaTitleMissingRule(),
        new MetaTitleLengthRule(),
        new MetaDescriptionMissingRule(),
        new MetaDescriptionLengthRule(),
        new H1MissingRule(),
        new H1MultipleRule(),
        new CanonicalRule(),
        new ThinContentRule(),
        new ImageAltRule(),
        new StructuredDataRule(),
    ]);

    public RuleEvaluation Evaluate(PageInput page)
    {
        var violations = new List<RuleViolation>();
        foreach (var rule in _rules)
        {
            var msg = rule.Evaluate(page);
            if (msg is not null)
                violations.Add(new RuleViolation(rule.Code, rule.Category, rule.Severity, rule.Weight, msg));
        }

        // Sayfa 2xx donmuyorsa icerik kurallarinin bulgusu gurultu — sadece durum kodunu bildir.
        if (page.StatusCode is < 200 or >= 300)
            violations = [.. violations.Where(v => v.Code == "HTTP_STATUS")];

        return new RuleEvaluation(ScoreCalculator.Calculate(violations), violations);
    }
}
