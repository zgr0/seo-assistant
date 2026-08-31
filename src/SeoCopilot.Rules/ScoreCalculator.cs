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

    /// <summary>Tek sayfa skoru.</summary>
    public static int Calculate(IEnumerable<RuleViolation> violations)
    {
        var total = violations.Sum(v => PenaltyFor(v.Severity));
        return Math.Clamp(100 - total, 0, 100);
    }

    /// <summary>
    /// Crawl geneli skor: sayfa skorlarinin ortalamasi, ardindan crawl seviyesi
    /// bulgularin cezasi dusulur.
    /// Ceza kural kodu basina bir kez uygulanir — ORPHAN_PAGE gibi sayfa basina
    /// bulgu ureten kurallar buyuk sitelerde skoru tek basina sifirlamasin.
    /// </summary>
    public static decimal CalculateOverall(
        IEnumerable<int> pageScores,
        IEnumerable<RuleViolation> crawlLevelViolations)
    {
        var scores = pageScores as IReadOnlyList<int> ?? [.. pageScores];
        if (scores.Count == 0) return 0m;

        var average = (decimal)scores.Sum() / scores.Count;
        var penalty = crawlLevelViolations
            .GroupBy(v => v.Code, StringComparer.Ordinal)
            .Sum(g => PenaltyFor(g.Max(v => v.Severity)));

        return Math.Clamp(Math.Round(average - penalty, 2), 0m, 100m);
    }

    /// <summary>
    /// Kategori bazli skorlar. Bir kategorinin cezasi tum sayfalara yayilir:
    /// her sayfada bir Medium meta ihlali varsa meta = 92.
    /// </summary>
    public static Dictionary<string, decimal> CalculateCategoryScores(
        IEnumerable<RuleViolation> allViolations,
        int pageCount)
    {
        var result = new Dictionary<string, decimal>();
        if (pageCount <= 0) return result;

        foreach (var group in allViolations.GroupBy(v => v.Category))
        {
            var penalty = (decimal)group.Sum(v => PenaltyFor(v.Severity)) / pageCount;
            result[CategoryKey(group.Key)] = Math.Clamp(Math.Round(100m - penalty, 2), 0m, 100m);
        }

        return result;
    }

    /// <summary>
    /// Skorlamada kullanilan agirlik tablosu. crawl.scoring_snapshot'a yazilir ki
    /// ceza puanlari ileride degisse bile gecmis crawl'lar yorumlanabilsin.
    /// </summary>
    public static Dictionary<string, int> Snapshot() =>
        Enum.GetValues<Severity>().ToDictionary(
            s => s.ToString().ToLowerInvariant(),
            PenaltyFor);

    /// <summary>RuleCategory -> snake_case anahtar (orn. StructuredData -> structured_data).</summary>
    public static string CategoryKey(RuleCategory category)
    {
        var name = category.ToString();
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i])) sb.Append('_');
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }
}
