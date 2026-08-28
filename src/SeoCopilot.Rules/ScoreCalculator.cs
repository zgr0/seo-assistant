using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Rules;

/// <summary>
/// Bulgu listesinden 0-100 skor uretir. Saf ve deterministik — en kritik test kumesi burayi hedefler.
/// Her severity sabit ceza puani tasir; toplam ceza 100'den dusulur, 0'a klemplenir.
/// </summary>
public static class ScoreCalculator
{
    public static int PenaltyFor(Severity s) => s switch
    {
        Severity.Critical => 25,
        Severity.High => 15,
        Severity.Medium => 8,
        Severity.Low => 3,
        Severity.Info => 0,
        _ => 0
    };

    public static int Calculate(IEnumerable<RuleViolation> violations)
    {
        var total = violations.Sum(v => PenaltyFor(v.Severity));
        return Math.Clamp(100 - total, 0, 100);
    }
}
