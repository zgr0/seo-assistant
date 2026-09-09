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

public sealed class MetaTitleTooShortRule : ISeoRule
{
    public const int Min = 30;

    public string Code => "META_TITLE_TOO_SHORT";
    public RuleCategory Category => RuleCategory.Meta;
    public Severity Severity => Severity.Medium;
    public int Weight => 5;

    public string? Evaluate(PageInput page)
    {
        var t = page.Title?.Trim();
        if (string.IsNullOrEmpty(t)) return null; // META_TITLE_MISSING ilgilenir

        return t.Length < Min
            ? $"Title çok kısa ({t.Length} krk). En az {Min} karakter olmalı."
            : null;
    }
}

public sealed class MetaTitleTooLongRule : ISeoRule
{
    public const int Max = 60;

    public string Code => "META_TITLE_TOO_LONG";
    public RuleCategory Category => RuleCategory.Meta;
    public Severity Severity => Severity.Medium;
    public int Weight => 5;

    public string? Evaluate(PageInput page)
    {
        var t = page.Title?.Trim();
        if (string.IsNullOrEmpty(t)) return null; // META_TITLE_MISSING ilgilenir

        return t.Length > Max
            ? $"Title çok uzun ({t.Length} krk). En fazla {Max} karakter olmalı."
            : null;
    }
}
