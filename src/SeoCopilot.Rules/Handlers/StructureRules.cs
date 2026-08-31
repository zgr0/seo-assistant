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

/// <summary>Baslik seviyeleri atlanmis mi — orn. h2'den sonra dogrudan h4.</summary>
public sealed class HeadingHierarchyBrokenRule : ISeoRule
{
    public string Code => "HEADING_HIERARCHY_BROKEN";
    public RuleCategory Category => RuleCategory.Content;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page)
    {
        var previous = 0;
        foreach (var level in page.HeadingLevels)
        {
            if (previous > 0 && level > previous + 1)
                return $"Baslik seviyesi atlanmis: h{previous} sonrasi h{level} geliyor.";

            previous = level;
        }

        return null;
    }
}
