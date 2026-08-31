using SeoCopilot.Rules.Handlers;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Tests;

public class RuleEngineTests
{
    private static PageInput HealthyPage() => new()
    {
        Url = "https://example.com/",
        StatusCode = 200,
        Title = "Ornek sayfa — kaliteli ve dogru uzunlukta baslik metni",
        MetaDescription = new string('a', 120),
        H1 = ["Tek baslik"],
        HeadingLevels = [1, 2, 3, 2],
        InternalLinks = ["/hakkinda"],
        InternalAnchorTexts = ["hakkimizda sayfasi"],
        WordCount = 500,
        HasCanonical = true,
        CanonicalUrl = "https://example.com/",
        ImagesTotal = 2,
        ImagesNoAlt = 0,
        ImageSizes = [new ImageSize("https://example.com/a.png", 40_000)],
        SchemaTypes = ["Article"],
        OgTags = ["og:title", "og:description", "og:image"],
        Lang = "tr"
    };

    [Fact]
    public void Healthy_page_scores_100_with_no_violations()
    {
        var result = RuleEngine.Default().Evaluate(HealthyPage());
        Assert.Empty(result.Violations);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Missing_title_triggers_seeded_rule_code()
    {
        var result = RuleEngine.Default().Evaluate(HealthyPage() with { Title = null });
        Assert.Contains(result.Violations, v => v.Code == "META_TITLE_MISSING");
    }

    [Fact]
    public void Title_length_is_split_into_short_and_long_codes()
    {
        var tooShort = RuleEngine.Default().Evaluate(HealthyPage() with { Title = "Kisa baslik" });
        Assert.Contains(tooShort.Violations, v => v.Code == "META_TITLE_TOO_SHORT");
        Assert.DoesNotContain(tooShort.Violations, v => v.Code == "META_TITLE_TOO_LONG");

        var tooLong = RuleEngine.Default().Evaluate(HealthyPage() with { Title = new string('a', 80) });
        Assert.Contains(tooLong.Violations, v => v.Code == "META_TITLE_TOO_LONG");
        Assert.DoesNotContain(tooLong.Violations, v => v.Code == "META_TITLE_TOO_SHORT");
    }

    [Fact]
    public void Multiple_h1_and_missing_h1_are_separate_codes()
    {
        var multiple = RuleEngine.Default().Evaluate(HealthyPage() with { H1 = ["bir", "iki"] });
        Assert.Contains(multiple.Violations, v => v.Code == "H1_MULTIPLE");
        Assert.DoesNotContain(multiple.Violations, v => v.Code == "H1_MISSING");

        var missing = RuleEngine.Default().Evaluate(HealthyPage() with { H1 = [] });
        Assert.Contains(missing.Violations, v => v.Code == "H1_MISSING");
        Assert.DoesNotContain(missing.Violations, v => v.Code == "H1_MULTIPLE");
    }

    [Fact]
    public void Meta_description_missing_and_too_long_are_separate_codes()
    {
        var missing = RuleEngine.Default().Evaluate(HealthyPage() with { MetaDescription = null });
        Assert.Contains(missing.Violations, v => v.Code == "META_DESC_MISSING");
        Assert.DoesNotContain(missing.Violations, v => v.Code == "META_DESC_TOO_LONG");

        var tooLong = RuleEngine.Default().Evaluate(
            HealthyPage() with { MetaDescription = new string('a', 200) });
        Assert.Contains(tooLong.Violations, v => v.Code == "META_DESC_TOO_LONG");
        Assert.DoesNotContain(tooLong.Violations, v => v.Code == "META_DESC_MISSING");

        // Kisa aciklama artik bulgu uretmiyor — yalniz ust sinir kuralli.
        var short_ = RuleEngine.Default().Evaluate(HealthyPage() with { MetaDescription = "kisa" });
        Assert.DoesNotContain(short_.Violations, v => v.Code.StartsWith("META_DESC"));
    }

    [Fact]
    public void Noindex_is_flagged()
    {
        var result = RuleEngine.Default().Evaluate(HealthyPage() with { RobotsMeta = "noindex, follow" });
        Assert.Contains(result.Violations, v => v.Code == "ROBOTS_NOINDEX");
    }

    [Fact]
    public void Canonical_missing_and_pointing_elsewhere_are_separate_codes()
    {
        var missing = RuleEngine.Default().Evaluate(
            HealthyPage() with { HasCanonical = false, CanonicalUrl = null });
        Assert.Contains(missing.Violations, v => v.Code == "CANONICAL_MISSING");
        Assert.DoesNotContain(missing.Violations, v => v.Code == "CANONICAL_POINTS_ELSEWHERE");

        var elsewhere = RuleEngine.Default().Evaluate(
            HealthyPage() with { CanonicalUrl = "https://example.com/baska" });
        Assert.Contains(elsewhere.Violations, v => v.Code == "CANONICAL_POINTS_ELSEWHERE");
        Assert.DoesNotContain(elsewhere.Violations, v => v.Code == "CANONICAL_MISSING");
    }

    [Fact]
    public void Self_referencing_canonical_ignores_the_trailing_slash()
    {
        var result = RuleEngine.Default().Evaluate(HealthyPage() with { CanonicalUrl = "https://example.com" });
        Assert.DoesNotContain(result.Violations, v => v.Code == "CANONICAL_POINTS_ELSEWHERE");
    }

    [Fact]
    public void Redirect_chain_needs_more_than_one_hop()
    {
        Assert.DoesNotContain(
            RuleEngine.Default().Evaluate(HealthyPage() with { RedirectCount = 1 }).Violations,
            v => v.Code == "REDIRECT_CHAIN");

        Assert.Contains(
            RuleEngine.Default().Evaluate(HealthyPage() with { RedirectCount = 3 }).Violations,
            v => v.Code == "REDIRECT_CHAIN");
    }

    [Fact]
    public void Skipped_heading_level_is_flagged()
    {
        var broken = RuleEngine.Default().Evaluate(HealthyPage() with { HeadingLevels = [1, 2, 4] });
        Assert.Contains(broken.Violations, v => v.Code == "HEADING_HIERARCHY_BROKEN");

        // Geri donmek (h3 → h2) hiyerarsiyi bozmaz.
        var fine = RuleEngine.Default().Evaluate(HealthyPage() with { HeadingLevels = [1, 2, 3, 2, 3] });
        Assert.DoesNotContain(fine.Violations, v => v.Code == "HEADING_HIERARCHY_BROKEN");
    }

    [Fact]
    public void Generic_anchor_text_is_flagged_regardless_of_turkish_diacritics()
    {
        var result = RuleEngine.Default().Evaluate(
            HealthyPage() with { InternalAnchorTexts = ["Buraya tıklayın", "urun listesi", "Read more"] });

        var finding = Assert.Single(result.Violations, v => v.Code == "GENERIC_ANCHOR_TEXT");
        Assert.Contains("buraya tiklayin", finding.Message);
        Assert.Contains("read more", finding.Message);
    }

    [Fact]
    public void Missing_alt_schema_og_and_lang_are_flagged()
    {
        var result = RuleEngine.Default().Evaluate(
            HealthyPage() with { ImagesNoAlt = 3, SchemaTypes = [], OgTags = [], Lang = null });

        Assert.Contains(result.Violations, v => v.Code == "IMAGE_MISSING_ALT");
        Assert.Contains(result.Violations, v => v.Code == "SCHEMA_MISSING");
        Assert.Contains(result.Violations, v => v.Code == "OG_TAGS_MISSING");
        Assert.Contains(result.Violations, v => v.Code == "LANG_ATTR_MISSING");
    }

    [Fact]
    public void Partial_open_graph_still_reports_the_missing_tags()
    {
        var result = RuleEngine.Default().Evaluate(HealthyPage() with { OgTags = ["og:title"] });

        var finding = Assert.Single(result.Violations, v => v.Code == "OG_TAGS_MISSING");
        Assert.Contains("og:image", finding.Message);
        Assert.DoesNotContain("og:title", finding.Message);
    }

    [Fact]
    public void Images_over_200_kb_are_flagged_only_when_measured()
    {
        var big = RuleEngine.Default().Evaluate(HealthyPage() with
        {
            ImageSizes = [new ImageSize("https://example.com/hero.jpg", 300 * 1024)]
        });
        Assert.Contains(big.Violations, v => v.Code == "IMAGE_TOO_LARGE");

        // Olcum yapilmadiysa kural sessiz kalir.
        var unmeasured = RuleEngine.Default().Evaluate(HealthyPage() with { ImageSizes = [] });
        Assert.DoesNotContain(unmeasured.Violations, v => v.Code == "IMAGE_TOO_LARGE");
    }

    [Fact]
    public void Non_2xx_page_reports_only_the_reachability_rule()
    {
        var result = RuleEngine.Default().Evaluate(
            HealthyPage() with { StatusCode = 404, Title = null, H1 = [], HasCanonical = false });

        Assert.Single(result.Violations);
        Assert.Equal("BROKEN_PAGE_4XX", result.Violations[0].Code);
    }

    [Fact]
    public void Server_error_and_unreachable_page_share_a_code()
    {
        Assert.Equal(
            "SERVER_ERROR_5XX",
            Assert.Single(RuleEngine.Default().Evaluate(HealthyPage() with { StatusCode = 503 }).Violations).Code);

        Assert.Equal(
            "SERVER_ERROR_5XX",
            Assert.Single(RuleEngine.Default().Evaluate(HealthyPage() with { StatusCode = 0 }).Violations).Code);
    }

    [Fact]
    public void Thin_content_rule_direct()
    {
        var rule = new ThinContentRule();
        Assert.NotNull(rule.Evaluate(HealthyPage() with { WordCount = 50 }));
        Assert.Null(rule.Evaluate(HealthyPage()));
    }

    [Fact]
    public void Every_rule_carries_a_weight_and_category()
    {
        var page = new PageInput { Url = "https://example.com/", StatusCode = 200 };
        var violations = RuleEngine.Default().Evaluate(page).Violations;

        Assert.NotEmpty(violations);
        Assert.All(violations, v => Assert.True(v.Weight > 0));
    }
}
