using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

/// <summary>4xx donen sayfa — dizinden dusen kirik icerik.</summary>
public sealed class BrokenPage4xxRule : ISeoRule
{
    public string Code => "BROKEN_PAGE_4XX";
    public RuleCategory Category => RuleCategory.Indexability;
    public Severity Severity => Severity.Critical;
    public int Weight => 10;

    public string? Evaluate(PageInput page) =>
        page.StatusCode is >= 400 and < 500 ? $"Sayfa {page.StatusCode} donuyor." : null;
}

/// <summary>5xx donen ya da hic getirilemeyen sayfa.</summary>
public sealed class ServerError5xxRule : ISeoRule
{
    public string Code => "SERVER_ERROR_5XX";
    public RuleCategory Category => RuleCategory.Indexability;
    public Severity Severity => Severity.Critical;
    public int Weight => 10;

    public string? Evaluate(PageInput page) => page.StatusCode switch
    {
        0 => "Sayfa getirilemedi (baglanti hatasi / zaman asimi).",
        >= 500 => $"Sayfa {page.StatusCode} donuyor.",
        _ => null
    };
}

/// <summary>Birden fazla yonlendirme atlamasi — link degeri her atlamada erir.</summary>
public sealed class RedirectChainRule : ISeoRule
{
    public const int MaxHops = 1;

    public string Code => "REDIRECT_CHAIN";
    public RuleCategory Category => RuleCategory.Indexability;
    public Severity Severity => Severity.Medium;
    public int Weight => 5;

    public string? Evaluate(PageInput page) =>
        page.RedirectCount > MaxHops
            ? $"Sayfaya {page.RedirectCount} yonlendirme sonrasi ulasiliyor (en fazla {MaxHops} olmali)."
            : null;
}

/// <summary>
/// Yonlendirme http/https disi bir hedefe gidiyor (orn. <c>Location: javascript:;</c>).
/// Sunucu yanit veriyor ama hicbir istemci hedefe gidemez; sayfa fiilen ulasilamaz.
/// </summary>
public sealed class RedirectTargetInvalidRule : ISeoRule
{
    public string Code => "REDIRECT_TARGET_INVALID";
    public RuleCategory Category => RuleCategory.Indexability;
    public Severity Severity => Severity.Critical;
    public int Weight => 9;

    public string? Evaluate(PageInput page) =>
        page.InvalidRedirectTarget is string target
            ? $"Yonlendirme HTTP olmayan bir hedefe gidiyor: {target}"
            : null;
}

/// <summary>meta[name=robots] veya X-Robots-Tag icinde noindex arar.</summary>
public sealed class RobotsNoIndexRule : ISeoRule
{
    public string Code => "ROBOTS_NOINDEX";
    public RuleCategory Category => RuleCategory.Indexability;
    public Severity Severity => Severity.Critical;
    public int Weight => 9;

    public string? Evaluate(PageInput page) =>
        page.RobotsMeta?.Contains("noindex", StringComparison.OrdinalIgnoreCase) == true
            ? $"robots direktifi noindex iceriyor: \"{page.RobotsMeta}\"."
            : null;
}

public sealed class CanonicalMissingRule : ISeoRule
{
    public string Code => "CANONICAL_MISSING";
    public RuleCategory Category => RuleCategory.Indexability;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page) =>
        page.HasCanonical ? null : "rel=canonical link yok.";
}

/// <summary>Canonical baska bir adresi gosteriyor — sayfa kendi basina dizine girmez.</summary>
public sealed class CanonicalPointsElsewhereRule : ISeoRule
{
    public string Code => "CANONICAL_POINTS_ELSEWHERE";
    public RuleCategory Category => RuleCategory.Indexability;
    public Severity Severity => Severity.Medium;
    public int Weight => 5;

    public string? Evaluate(PageInput page)
    {
        if (string.IsNullOrWhiteSpace(page.CanonicalUrl)) return null; // CANONICAL_MISSING ilgilenir

        return SameTarget(page.CanonicalUrl, page.Url)
            ? null
            : $"Canonical baska adresi gosteriyor: {page.CanonicalUrl}.";
    }

    /// <summary>Sondaki tek '/' disinda birebir ayni mi.</summary>
    private static bool SameTarget(string a, string b) =>
        string.Equals(a.TrimEnd('/'), b.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
}
