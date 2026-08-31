using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

public sealed class MetaTitleMissingRule : ISeoRule
{
    public string Code => "META_TITLE_MISSING";
    public RuleCategory Category => RuleCategory.Meta;
    public Severity Severity => Severity.Critical;
    public int Weight => 9;

    public string? Evaluate(PageInput page) =>
        string.IsNullOrWhiteSpace(page.Title) ? "Sayfada <title> etiketi yok." : null;
}

public sealed class MetaTitleLengthRule : ISeoRule
{
    public const int Min = 30;
    public const int Max = 60;

    public string Code => "META_TITLE_LENGTH";
    public RuleCategory Category => RuleCategory.Meta;
    public Severity Severity => Severity.Medium;
    public int Weight => 5;

    public string? Evaluate(PageInput page)
    {
        var t = page.Title?.Trim();
        if (string.IsNullOrEmpty(t)) return null; // META_TITLE_MISSING ilgilenir
        return t.Length switch
        {
            < Min => $"Title cok kisa ({t.Length} krk). Onerilen {Min}-{Max}.",
            > Max => $"Title cok uzun ({t.Length} krk). Onerilen {Min}-{Max}.",
            _ => null
        };
    }
}
