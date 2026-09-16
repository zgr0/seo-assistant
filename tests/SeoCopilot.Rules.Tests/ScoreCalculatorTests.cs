using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules;

namespace SeoCopilot.Rules.Tests;

public class ScoreCalculatorTests
{
    private static RuleViolation V(
        Severity severity, RuleCategory category = RuleCategory.Meta, string code = "X") =>
        new(code, category, severity, 1, "msg");

    /// <summary>Her kategorisi ayni skorda bir tablo — agirlikli eksik = 100 - score.</summary>
    private static Dictionary<string, decimal> AllCategories(decimal score) =>
        ScoreCalculator.CategoryWeights.Keys.ToDictionary(ScoreCalculator.CategoryKey, _ => score);

    [Fact]
    public void No_violations_is_100()
    {
        Assert.Equal(100, ScoreCalculator.Calculate([]));
    }

    [Theory]
    [InlineData(Severity.Critical, 75)]
    [InlineData(Severity.High, 85)]
    [InlineData(Severity.Medium, 92)]
    [InlineData(Severity.Low, 97)]
    [InlineData(Severity.Info, 100)]
    public void Single_violation_applies_severity_penalty(Severity severity, int expected)
    {
        Assert.Equal(expected, ScoreCalculator.Calculate([V(severity)]));
    }

    [Fact]
    public void Penalties_accumulate()
    {
        RuleViolation[] violations =
        [
            V(Severity.Critical), // -25
            V(Severity.High),     // -15
            V(Severity.Medium),   // -8
        ];
        Assert.Equal(52, ScoreCalculator.Calculate(violations));
    }

    [Fact]
    public void Score_is_clamped_at_zero()
    {
        var violations = Enumerable.Range(0, 10).Select(_ => V(Severity.Critical)).ToArray();
        Assert.Equal(0, ScoreCalculator.Calculate(violations));
    }

    [Fact]
    public void Category_scores_spread_page_penalty_across_pages()
    {
        // 2 sayfa, meta kategorisinde tek Medium ihlal (-8) → 100 - 8/2 = 96
        var scores = ScoreCalculator.CalculateCategoryScores([V(Severity.Medium)], [], pageCount: 2);

        Assert.Equal(96m, scores["meta"]);
    }

    [Fact]
    public void Category_scores_cover_every_category()
    {
        var scores = ScoreCalculator.CalculateCategoryScores([V(Severity.Medium)], [], pageCount: 2);

        Assert.Equal(ScoreCalculator.CategoryWeights.Count, scores.Count);
        Assert.Equal(100m, scores["images"]);
        Assert.Equal(100m, scores["i18n"]);
    }

    [Fact]
    public void Category_scores_do_not_divide_crawl_level_penalty()
    {
        // SITEMAP_MISSING site capinda tek bir gercek — 60 sayfaya bolunmez.
        var scores = ScoreCalculator.CalculateCategoryScores(
            [], [V(Severity.Medium, RuleCategory.Indexability, "SITEMAP_MISSING")], pageCount: 60);

        Assert.Equal(92m, scores["indexability"]);
    }

    [Fact]
    public void Category_scores_penalise_each_crawl_rule_code_once()
    {
        // Ayni kuraldan 5 bulgu (orn. 5 oksuz sayfa) tek bir High cezasi kadar dusurur.
        RuleViolation[] orphans =
            [.. Enumerable.Range(0, 5).Select(_ => V(Severity.High, RuleCategory.Links, "ORPHAN_PAGE"))];

        var scores = ScoreCalculator.CalculateCategoryScores([], orphans, pageCount: 5);

        Assert.Equal(85m, scores["links"]);
    }

    [Fact]
    public void Category_scores_add_page_and_crawl_penalties()
    {
        // 2 sayfa: sayfa seviyesi Medium (-8/2 = -4) + crawl seviyesi Low (-3) → 93
        var scores = ScoreCalculator.CalculateCategoryScores(
            [V(Severity.Medium)], [V(Severity.Low, RuleCategory.Meta, "META_DESC_DUPLICATE")], pageCount: 2);

        Assert.Equal(93m, scores["meta"]);
    }

    [Fact]
    public void Category_scores_are_keyed_by_snake_case()
    {
        var scores = ScoreCalculator.CalculateCategoryScores(
            [V(Severity.Low, RuleCategory.StructuredData)], [], pageCount: 1);

        Assert.True(scores.ContainsKey("structured_data"));
        Assert.Equal(97m, scores["structured_data"]);
    }

    [Fact]
    public void Category_scores_without_pages_are_empty()
    {
        Assert.Empty(ScoreCalculator.CalculateCategoryScores([V(Severity.High)], [], pageCount: 0));
    }

    [Fact]
    public void Overall_is_100_when_nothing_is_wrong()
    {
        var scores = ScoreCalculator.CalculateCategoryScores([], [], pageCount: 10);

        Assert.Equal(100m, ScoreCalculator.CalculateOverall(scores));
    }

    [Fact]
    public void Overall_is_50_at_the_half_point()
    {
        // Dizinlenebilirlik (agirlik 20) 10 → agirlikli eksik 90*20/100 = 18 = esik.
        var scores = AllCategories(100m);
        scores["indexability"] = 10m;

        Assert.Equal(18m, (decimal)ScoreCalculator.OverallHalfPoint);
        Assert.Equal(50m, ScoreCalculator.CalculateOverall(scores));
    }

    [Fact]
    public void Overall_weights_categories()
    {
        // Yalniz dizinlenebilirlik 0 (agirlik 20/100) → eksik 20 → 100 - 100*20/38
        var scores = AllCategories(100m);
        scores["indexability"] = 0m;

        Assert.Equal(47.37m, ScoreCalculator.CalculateOverall(scores));
    }

    [Fact]
    public void Overall_does_not_stack_category_deficits()
    {
        // Her kategori 92 → eksik 8 → 69.23. Eski toplamsal model 100-8*8 = 36 verirdi.
        Assert.Equal(69.23m, ScoreCalculator.CalculateOverall(AllCategories(92m)));
    }

    [Fact]
    public void Overall_curve_amplifies_small_deficits()
    {
        // Duz agirlikli ortalama 92 verirdi; egri kucuk eksigi buyutup 69.23'e ceker.
        var scores = AllCategories(92m);

        Assert.True(ScoreCalculator.CalculateOverall(scores) < 92m);
    }

    [Fact]
    public void Overall_never_collapses_to_zero_and_keeps_order()
    {
        // Egri tabana yapismaz: agir hasarli iki site ayni skoru almaz.
        var bad = ScoreCalculator.CalculateOverall(AllCategories(50m));   // eksik 50
        var worse = ScoreCalculator.CalculateOverall(AllCategories(20m)); // eksik 80

        Assert.Equal(26.47m, bad);
        Assert.Equal(18.37m, worse);
        Assert.True(worse < bad);
    }

    [Fact]
    public void Overall_treats_missing_category_as_clean()
    {
        // Tabloda yalniz meta var (agirlik 18) → eksik 18*50/100 = 9 → 100 - 100*9/27
        var scores = new Dictionary<string, decimal> { ["meta"] = 50m };

        Assert.Equal(66.67m, ScoreCalculator.CalculateOverall(scores));
    }

    [Fact]
    public void Overall_without_categories_is_zero()
    {
        Assert.Equal(0m, ScoreCalculator.CalculateOverall(new Dictionary<string, decimal>()));
    }

    [Fact]
    public void Crawl_level_finding_is_counted_once()
    {
        // Bulgu yalniz linkler kategorisinde sayilir; genel skor bu tablodan turer.
        RuleViolation[] crawlLevel = [V(Severity.High, RuleCategory.Links, "BROKEN_INTERNAL_LINK")];

        var scores = ScoreCalculator.CalculateCategoryScores([], crawlLevel, pageCount: 10);

        Assert.Equal(85m, scores["links"]);
        Assert.Equal(89.55m, ScoreCalculator.CalculateOverall(scores)); // eksik 14*15/100 = 2.1
    }

    [Fact]
    public void Snapshot_carries_the_penalty_table()
    {
        var snapshot = ScoreCalculator.Snapshot();

        Assert.Equal(25, snapshot["critical"]);
        Assert.Equal(15, snapshot["high"]);
        Assert.Equal(8, snapshot["medium"]);
        Assert.Equal(3, snapshot["low"]);
        Assert.Equal(0, snapshot["info"]);
    }

    [Fact]
    public void Snapshot_carries_the_category_weights()
    {
        var snapshot = ScoreCalculator.Snapshot();

        Assert.Equal(20, snapshot["category_weight_indexability"]);
        Assert.Equal(4, snapshot["category_weight_i18n"]);
        Assert.Equal(100, ScoreCalculator.CategoryWeights.Values.Sum());
    }

    [Fact]
    public void Snapshot_carries_the_overall_curve_threshold()
    {
        Assert.Equal(ScoreCalculator.OverallHalfPoint, ScoreCalculator.Snapshot()["overall_half_point"]);
    }
}
