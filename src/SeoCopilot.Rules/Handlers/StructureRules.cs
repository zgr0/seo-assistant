using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

public sealed class H1MissingRule : ISeoRule
{
    public string Code => "H1_MISSING";
    public RuleCategory Category => RuleCategory.Content;
    public Severity Severity => Severity.High;
    public int Weight => 6;

    public string? Evaluate(PageInput page) =>
        page.H1.Count == 0 ? "Sayfada H1 yok." : null;
}

public sealed class H1MultipleRule : ISeoRule
{
    public string Code => "H1_MULTIPLE";
    public RuleCategory Category => RuleCategory.Content;
    public Severity Severity => Severity.Medium;
    public int Weight => 4;

    public string? Evaluate(PageInput page) =>
        page.H1.Count > 1 ? $"Sayfada {page.H1.Count} adet H1 var, tek olmali." : null;
}

public sealed class CanonicalRule : ISeoRule
{
    public string Code => "CANONICAL_MISSING";
    public RuleCategory Category => RuleCategory.Indexability;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page) =>
        page.HasCanonical ? null : "rel=canonical link yok.";
}

public sealed class ThinContentRule : ISeoRule
{
    public const int MinWords = 300;

    public string Code => "THIN_CONTENT";
    public RuleCategory Category => RuleCategory.Content;
    public Severity Severity => Severity.Medium;
    public int Weight => 5;

    public string? Evaluate(PageInput page) =>
        page.WordCount < MinWords
            ? $"Icerik zayif ({page.WordCount} kelime, min {MinWords})."
            : null;
}

public sealed class HttpStatusRule : ISeoRule
{
    public string Code => "HTTP_STATUS";
    public RuleCategory Category => RuleCategory.Indexability;
    public Severity Severity => Severity.Critical;
    public int Weight => 10;

    public string? Evaluate(PageInput page) => page.StatusCode switch
    {
        >= 200 and < 300 => null,
        0 => "Sayfa getirilemedi (baglanti hatasi / zaman asimi).",
        var s => $"Sayfa {s} donuyor."
    };
}
