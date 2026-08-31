using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

/// <summary>meta[name=robots] veya X-Robots-Tag icinde noindex arar.</summary>
public sealed class NoIndexRule : ISeoRule
{
    public string Code => "NOINDEX_DETECTED";
    public RuleCategory Category => RuleCategory.Indexability;
    public Severity Severity => Severity.Critical;
    public int Weight => 9;

    public string? Evaluate(PageInput page) =>
        page.RobotsMeta?.Contains("noindex", StringComparison.OrdinalIgnoreCase) == true
            ? $"robots direktifi noindex iceriyor: \"{page.RobotsMeta}\"."
            : null;
}
