using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Rules;

/// <summary>
/// Bulgu listesinden 0-100 skor uretir. Saf ve deterministik — en kritik test kumesi burayi hedefler.
/// Her severity sabit ceza puani tasir; toplam ceza 100'den dusulur, 0'a klemplenir.
///
/// Skor hiyerarsisi tek yonlu: bulgu -> kategori skoru -> genel skor. Bir bulgu yalnizca
/// bir yerde sayilir; genel skor kategorilerden turetilir ve ayrica ceza yemez. Once
/// kategoriler bagimsiz (her biri 100'den baslar), genel skor ise tum cezalari toplardi —
/// sekiz kategorinin her birinde ufak bir eksik, genel skoru sifira yapistiriyordu.
/// </summary>
public static class ScoreCalculator
{
    /// <summary>
    /// Genel skorun agirlikli ortalamasinda kullanilan kategori agirliklari. Toplam 100.
    /// Dizinlenebilirlik en agiri: sayfa dizine girmiyorsa geri kalani bir sey ifade etmez.
    /// </summary>
    public static readonly IReadOnlyDictionary<RuleCategory, int> CategoryWeights =
        new Dictionary<RuleCategory, int>
        {
            [RuleCategory.Indexability] = 20,
            [RuleCategory.Meta] = 18,
            [RuleCategory.Content] = 18,
            [RuleCategory.Links] = 14,
            [RuleCategory.Performance] = 12,
            [RuleCategory.Images] = 8,
            [RuleCategory.StructuredData] = 6,
            [RuleCategory.I18n] = 4
        };

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
    /// Kategori bazli skorlar. Ihlali olmayan kategori de uretilir (100) — genel skor
    /// agirlikli ortalama aldigi icin tablonun eksiksiz olmasi gerekir.
    ///
    /// Sayfa bulgusunun cezasi sayfa sayisina bolunur: her sayfada bir Medium meta
    /// ihlali varsa meta = 92, 60 sayfanin ikisinde varsa meta = 99.73.
    ///
    /// Crawl seviyesi bulgu bolunmez, kural kodu basina bir kez tam ceza yazilir.
    /// SITEMAP_MISSING site capinda tek bir gercektir; sayfa sayisina bolununce
    /// gorunmez hale gelirdi. Kod basina teklestirme de ORPHAN_PAGE gibi sayfa basina
    /// bulgu ureten kurallarin kategoriyi tek basina sifirlamasini engeller.
    /// </summary>
    public static Dictionary<string, decimal> CalculateCategoryScores(
        IEnumerable<RuleViolation> pageViolations,
        IEnumerable<RuleViolation> crawlLevelViolations,
        int pageCount)
    {
        var result = new Dictionary<string, decimal>();
        if (pageCount <= 0) return result;

        var penalties = new Dictionary<RuleCategory, decimal>();

        foreach (var group in pageViolations.GroupBy(v => v.Category))
            penalties[group.Key] = (decimal)group.Sum(v => PenaltyFor(v.Severity)) / pageCount;

        foreach (var group in crawlLevelViolations.GroupBy(v => v.Category))
        {
            var penalty = group
                .GroupBy(v => v.Code, StringComparer.Ordinal)
                .Sum(byCode => PenaltyFor(byCode.Max(v => v.Severity)));

            penalties[group.Key] = penalties.GetValueOrDefault(group.Key) + penalty;
        }

        foreach (var category in CategoryWeights.Keys)
            result[CategoryKey(category)] =
                Math.Clamp(Math.Round(100m - penalties.GetValueOrDefault(category), 2), 0m, 100m);

        return result;
    }

    /// <summary>
    /// Genel skorun tam 50'ye dustugu agirlikli eksik puan. Doygunluk egrisinin tek
    /// parametresi — kucultmek skoru sertlestirir, buyutmek yumusatir.
    /// Egrinin ürettigi degerler (agirlikli eksik → genel skor):
    /// 0 → 100 · 2 → 90 · 5 → 78.3 · 10 → 64.3 · 18 → 50 · 30 → 37.5 · 50 → 26.5
    /// </summary>
    public const int OverallHalfPoint = 18;

    /// <summary>
    /// Crawl geneli skor: kategori skorlarinin agirlikli eksigi, doygunluk egrisinden gecirilir.
    /// Bulgular zaten kategori skorlarinda sayildigi icin burada ikinci bir ceza uygulanmaz.
    /// Tabloda olmayan kategori 100 sayilir — o kategoride hicbir ihlal yok demektir.
    ///
    /// Duz agirlikli ortalama fazla comertti: temiz kategoriler (gorseller, dil) sorunlu
    /// olanlari seyreltip eksik sitemap + kirik link + duplicate icerik tasiyan siteye 88
    /// verdiriyordu. Egri kucuk eksikleri buyutur, buyukleri sikistirir; tabana yapismadigi
    /// icin kotu siteler arasindaki sira da korunur (eksik 50 ile 80 ayni skoru vermez).
    /// </summary>
    public static decimal CalculateOverall(IReadOnlyDictionary<string, decimal> categoryScores)
    {
        // Bos tablo = hic sayfa taranmadi. 100 donmek yaniltici olurdu.
        if (categoryScores.Count == 0) return 0m;

        decimal deficit = 0m;
        var totalWeight = 0;

        foreach (var (category, weight) in CategoryWeights)
        {
            deficit += (100m - categoryScores.GetValueOrDefault(CategoryKey(category), 100m)) * weight;
            totalWeight += weight;
        }

        deficit /= totalWeight;
        if (deficit <= 0m) return 100m;

        var penalty = 100m * deficit / (deficit + OverallHalfPoint);
        return Math.Clamp(Math.Round(100m - penalty, 2), 0m, 100m);
    }

    /// <summary>
    /// Skorlamada kullanilan agirlik tablosu. crawl.scoring_snapshot'a yazilir ki
    /// ceza puanlari ileride degisse bile gecmis crawl'lar yorumlanabilsin.
    /// Severity cezalari duz anahtarla ("critical"), kategori agirliklari
    /// "category_weight_" onekiyle ("category_weight_meta"), genel skor egrisinin
    /// esigi "overall_half_point" ile yazilir.
    /// </summary>
    public static Dictionary<string, int> Snapshot()
    {
        var snapshot = Enum.GetValues<Severity>().ToDictionary(
            s => s.ToString().ToLowerInvariant(),
            PenaltyFor);

        foreach (var (category, weight) in CategoryWeights)
            snapshot[$"category_weight_{CategoryKey(category)}"] = weight;

        snapshot["overall_half_point"] = OverallHalfPoint;

        return snapshot;
    }

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
