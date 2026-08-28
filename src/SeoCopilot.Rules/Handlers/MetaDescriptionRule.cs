using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

public sealed class MetaDescriptionRule : ISeoRule
{
    public const int Min = 70;
    public const int Max = 160;

    public string Code => "META_DESCRIPTION";
    public Severity Severity => Severity.Medium;

    public string? Evaluate(PageInput page)
    {
        var d = page.MetaDescription?.Trim();
        if (string.IsNullOrEmpty(d))
            return "Meta description yok.";
        return d.Length switch
        {
            < Min => $"Meta description cok kisa ({d.Length} krk).",
            > Max => $"Meta description cok uzun ({d.Length} krk).",
            _ => null
        };
    }
}
