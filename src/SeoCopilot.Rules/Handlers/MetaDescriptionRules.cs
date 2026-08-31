using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

public sealed class MetaDescMissingRule : ISeoRule
{
    public string Code => "META_DESC_MISSING";
    public RuleCategory Category => RuleCategory.Meta;
    public Severity Severity => Severity.High;
    public int Weight => 6;

    public string? Evaluate(PageInput page) =>
        string.IsNullOrWhiteSpace(page.MetaDescription) ? "Meta description yok." : null;
}

public sealed class MetaDescTooLongRule : ISeoRule
{
    public const int Max = 160;

    public string Code => "META_DESC_TOO_LONG";
    public RuleCategory Category => RuleCategory.Meta;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page)
    {
        var d = page.MetaDescription?.Trim();
        if (string.IsNullOrEmpty(d)) return null; // META_DESC_MISSING ilgilenir

        return d.Length > Max
            ? $"Meta description cok uzun ({d.Length} krk). En fazla {Max} karakter olmali."
            : null;
    }
}
