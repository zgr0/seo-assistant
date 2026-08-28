using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

public sealed class SingleH1Rule : ISeoRule
{
    public string Code => "H1_COUNT";
    public Severity Severity => Severity.High;

    public string? Evaluate(PageInput page) => page.H1.Count switch
    {
        0 => "Sayfada H1 yok.",
        1 => null,
        var n => $"Sayfada {n} adet H1 var, tek olmali."
    };
}

public sealed class CanonicalRule : ISeoRule
{
    public string Code => "CANONICAL_MISSING";
    public Severity Severity => Severity.Low;

    public string? Evaluate(PageInput page) =>
        page.HasCanonical ? null : "rel=canonical link yok.";
}

public sealed class ThinContentRule : ISeoRule
{
    public const int MinWords = 300;

    public string Code => "THIN_CONTENT";
    public Severity Severity => Severity.Medium;

    public string? Evaluate(PageInput page) =>
        page.WordCount < MinWords
            ? $"Icerik zayif ({page.WordCount} kelime, min {MinWords})."
            : null;
}

public sealed class HttpStatusRule : ISeoRule
{
    public string Code => "HTTP_STATUS";
    public Severity Severity => Severity.Critical;

    public string? Evaluate(PageInput page) =>
        page.StatusCode is >= 200 and < 300
            ? null
            : $"Sayfa {page.StatusCode} donuyor.";
}
