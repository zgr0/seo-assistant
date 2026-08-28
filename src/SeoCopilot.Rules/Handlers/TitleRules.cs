using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

public sealed class TitleMissingRule : ISeoRule
{
    public string Code => "TITLE_MISSING";
    public Severity Severity => Severity.Critical;

    public string? Evaluate(PageInput page) =>
        string.IsNullOrWhiteSpace(page.Title) ? "Sayfada <title> etiketi yok." : null;
}

public sealed class TitleLengthRule : ISeoRule
{
    public const int Min = 30;
    public const int Max = 60;

    public string Code => "TITLE_LENGTH";
    public Severity Severity => Severity.Medium;

    public string? Evaluate(PageInput page)
    {
        var t = page.Title?.Trim();
        if (string.IsNullOrEmpty(t)) return null; // TITLE_MISSING ilgilenir
        return t.Length switch
        {
            < Min => $"Title cok kisa ({t.Length} krk). Onerilen {Min}-{Max}.",
            > Max => $"Title cok uzun ({t.Length} krk). Onerilen {Min}-{Max}.",
            _ => null
        };
    }
}
