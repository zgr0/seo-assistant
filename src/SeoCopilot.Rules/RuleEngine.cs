using SeoCopilot.Rules.Handlers;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules;

/// <summary>
/// Tum kurallari sirayla calistirir, ihlalleri toplar, skoru hesaplar.
/// Stateless — bir kez kurulup her yerde paylasilabilir.
/// </summary>
public sealed class RuleEngine
{
    /// <summary>Sayfa 2xx donmuyorsa yalniz bu kodlar raporlanir; gerisi gurultu olurdu.</summary>
    private static readonly HashSet<string> TransportCodes =
        ["BROKEN_PAGE_4XX", "SERVER_ERROR_5XX", "REDIRECT_CHAIN", "REDIRECT_TARGET_INVALID"];

    private readonly IReadOnlyList<ISeoRule> _rules;

    public RuleEngine(IEnumerable<ISeoRule> rules) => _rules = [.. rules];

    /// <summary>Varsayilan kural seti — kodlar rules tablosu seed'i ile birebir ayni.</summary>
    public static RuleEngine Default() => new(
    [
        // Indexability
        new BrokenPage4xxRule(),
        new ServerError5xxRule(),
        new RedirectChainRule(),
        new RedirectTargetInvalidRule(),
        new RobotsNoIndexRule(),
        new CanonicalMissingRule(),
        new CanonicalPointsElsewhereRule(),

        // Meta
        new MetaTitleMissingRule(),
        new MetaTitleTooShortRule(),
        new MetaTitleTooLongRule(),
        new MetaDescMissingRule(),
        new MetaDescTooLongRule(),

        // Content
        new H1MissingRule(),
        new H1MultipleRule(),
        new ThinContentRule(),
        new HeadingHierarchyBrokenRule(),

        // Links
        new GenericAnchorTextRule(),

        // Images
        new ImageMissingAltRule(),
        new ImageTooLargeRule(),

        // Structured data & i18n
        new SchemaMissingRule(),
        new InvalidStructuredDataRule(),
        new OgTagsMissingRule(),
        new LangAttrMissingRule(),
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

        // Sayfa 2xx donmuyorsa icerik kurallarinin bulgusu gurultu — sadece ulasilabilirligi bildir.
        // Yonlendirilen URL'de de ayni sey gecerli: govde hedefe ait, icerik orada olculur.
        if (page.StatusCode is < 200 or >= 300 || page.IsRedirect)
            violations = [.. violations.Where(v => TransportCodes.Contains(v.Code))];

        return new RuleEvaluation(ScoreCalculator.Calculate(violations), violations);
    }
}
