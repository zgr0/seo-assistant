using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

public sealed class MetaDescriptionMissingRule : ISeoRule
{
    public string Code => "META_DESCRIPTION_MISSING";
    public RuleCategory Category => RuleCategory.Meta;
    public Severity Severity => Severity.High;
    public int Weight => 6;

    public string? Evaluate(PageInput page) =>
        string.IsNullOrWhiteSpace(page.MetaDescription) ? "Meta description yok." : null;
}

public sealed class MetaDescriptionLengthRule : ISeoRule
{
    public const int Min = 70;
    public const int Max = 160;

    public string Code => "META_DESCRIPTION_LENGTH";
    public RuleCategory Category => RuleCategory.Meta;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page)
    {
        var d = page.MetaDescription?.Trim();
        if (string.IsNullOrEmpty(d)) return null; // META_DESCRIPTION_MISSING ilgilenir
        return d.Length switch
        {
            < Min => $"Meta description cok kisa ({d.Length} krk). Onerilen {Min}-{Max}.",
            > Max => $"Meta description cok uzun ({d.Length} krk). Onerilen {Min}-{Max}.",
            _ => null
        };
    }
}
