using System.Text;

namespace SeoCopilot.Rules.Tests;

public class CrawlRulesTests
{
    private static readonly Guid PageA = Guid.CreateVersion7();
    private static readonly Guid PageB = Guid.CreateVersion7();
    private static readonly Guid PageC = Guid.CreateVersion7();

    private static byte[] Hash(string s) => Encoding.UTF8.GetBytes(s);

    /// <summary>Kok sayfa: ic linki var, sitemap'te, dizinlenebilir.</summary>
    private static CrawlPageInput Home(Guid id, string url = "https://example.com/") =>
        new(id, url, 200, Hash(url)) { IsHome = true, InlinkCount = 1 };

    [Fact]
    public void Identical_content_hashes_produce_one_duplicate_finding()
    {
        CrawlPageInput[] pages =
        [
            new(PageA, "https://example.com/a", 200, Hash("same")) { InlinkCount = 1 },
            new(PageB, "https://example.com/b", 200, Hash("same")) { InlinkCount = 1 },
            new(PageC, "https://example.com/c", 200, Hash("other")) { InlinkCount = 1 },
        ];

        var findings = CrawlRules.Evaluate(pages, []);

        var duplicate = Assert.Single(findings, f => f.Finding.Code == "DUPLICATE_CONTENT");
        Assert.Equal(2, duplicate.Finding.SampleUrls.Count);
        Assert.Contains("https://example.com/a", duplicate.Finding.SampleUrls);
        Assert.Contains("https://example.com/b", duplicate.Finding.SampleUrls);
    }

    [Fact]
    public void Non_2xx_pages_are_excluded_from_duplicate_detection()
    {
        CrawlPageInput[] pages =
        [
            new(PageA, "https://example.com/a", 404, Hash("same")),
            new(PageB, "https://example.com/b", 404, Hash("same")),
        ];

        Assert.Empty(CrawlRules.Evaluate(pages, []));
    }

    [Fact]
    public void Duplicate_titles_and_descriptions_are_reported_per_group()
    {
        CrawlPageInput[] pages =
        [
            new(PageA, "https://example.com/a", 200, null)
                { InlinkCount = 1, Title = "Ayni baslik", MetaDescription = "Ayni aciklama" },
            new(PageB, "https://example.com/b", 200, null)
                { InlinkCount = 1, Title = "Ayni baslik", MetaDescription = "Ayni aciklama" },
            new(PageC, "https://example.com/c", 200, null)
                { InlinkCount = 1, Title = "Baska baslik", MetaDescription = "Baska aciklama" },
        ];

        var findings = CrawlRules.Evaluate(pages, []);

        var title = Assert.Single(findings, f => f.Finding.Code == "META_TITLE_DUPLICATE");
        Assert.Equal(2, title.Finding.SampleUrls.Count);
        Assert.Single(findings, f => f.Finding.Code == "META_DESC_DUPLICATE");
    }

    [Fact]
    public void Noindex_pages_are_ignored_by_duplicate_meta_rules()
    {
        CrawlPageInput[] pages =
        [
            new(PageA, "https://example.com/a", 200, null) { InlinkCount = 1, Title = "Ayni baslik" },
            new(PageB, "https://example.com/b", 200, null) { InlinkCount = 1, Title = "Ayni baslik", NoIndex = true },
        ];

        Assert.DoesNotContain(CrawlRules.Evaluate(pages, []), f => f.Finding.Code == "META_TITLE_DUPLICATE");
    }

    [Fact]
    public void Broken_internal_links_are_grouped_per_source_page()
    {
        CrawlLinkInput[] links =
        [
            new(PageA, "https://example.com/a", "https://example.com/x", true, 404),
            new(PageA, "https://example.com/a", "https://example.com/y", true, 500),
            new(PageA, "https://example.com/a", "https://example.com/ok", true, 200),
            new(PageB, "https://example.com/b", "https://other.com/z", false, 404),
        ];

        var findings = CrawlRules.Evaluate([], links);

        var broken = Assert.Single(findings, f => f.Finding.Code == "BROKEN_INTERNAL_LINK");
        Assert.Equal(PageA, broken.PageId);
        Assert.Equal(2, broken.Finding.SampleUrls.Count);
    }

    [Fact]
    public void Uncrawled_link_targets_are_not_judged()
    {
        CrawlLinkInput[] links =
        [
            new(PageA, "https://example.com/a", "https://example.com/unknown", true, null),
        ];

        Assert.Empty(CrawlRules.Evaluate([], links));
    }

    [Fact]
    public void Unreachable_target_counts_as_broken()
    {
        CrawlLinkInput[] links =
        [
            new(PageA, "https://example.com/a", "https://example.com/dead", true, 0),
        ];

        Assert.Single(CrawlRules.Evaluate([], links));
    }

    [Fact]
    public void Pages_without_inlinks_are_orphans_except_the_home_page()
    {
        CrawlPageInput[] pages =
        [
            Home(PageA),
            new(PageB, "https://example.com/b", 200, null) { InlinkCount = 0 },
            new(PageC, "https://example.com/c", 200, null) { InlinkCount = 2 },
        ];

        var orphan = Assert.Single(CrawlRules.Evaluate(pages, []), f => f.Finding.Code == "ORPHAN_PAGE");
        Assert.Equal(PageB, orphan.PageId);
    }

    [Fact]
    public void Pages_deeper_than_four_clicks_are_flagged()
    {
        CrawlPageInput[] pages =
        [
            new(PageA, "https://example.com/a", 200, null) { InlinkCount = 1, Depth = 4 },
            new(PageB, "https://example.com/b", 200, null) { InlinkCount = 1, Depth = 5 },
        ];

        var deep = Assert.Single(CrawlRules.Evaluate(pages, []), f => f.Finding.Code == "TOO_DEEP");
        Assert.Equal(PageB, deep.PageId);
    }

    [Fact]
    public void Missing_sitemap_is_reported_once_and_suppresses_the_per_page_rule()
    {
        CrawlPageInput[] pages = [Home(PageA)];
        var site = new CrawlSiteInput { SitemapFound = false, HomePageId = PageA };

        var findings = CrawlRules.Evaluate(pages, [], site);

        Assert.Single(findings, f => f.Finding.Code == "SITEMAP_MISSING");
        Assert.DoesNotContain(findings, f => f.Finding.Code == "PAGE_NOT_IN_SITEMAP");
    }

    [Fact]
    public void Indexable_pages_outside_the_sitemap_are_flagged()
    {
        CrawlPageInput[] pages =
        [
            Home(PageA),
            new(PageB, "https://example.com/b", 200, null) { InlinkCount = 1 },
            new(PageC, "https://example.com/c", 200, null) { InlinkCount = 1, NoIndex = true },
        ];

        var site = new CrawlSiteInput
        {
            SitemapFound = true,
            SitemapUrls = ["https://example.com/", "https://example.com/c"],
            HomePageId = PageA
        };

        var missing = Assert.Single(
            CrawlRules.Evaluate(pages, [], site),
            f => f.Finding.Code == "PAGE_NOT_IN_SITEMAP");

        Assert.Equal(PageB, missing.PageId);
    }

    [Fact]
    public void Robots_blocked_urls_produce_one_site_level_finding()
    {
        var site = new CrawlSiteInput
        {
            SitemapFound = true,
            SitemapUrls = ["https://example.com/"],
            BlockedUrls = ["https://example.com/gizli/a", "https://example.com/gizli/b"],
            HomePageId = PageA
        };

        var blocked = Assert.Single(
            CrawlRules.Evaluate([Home(PageA)], [], site),
            f => f.Finding.Code == "BLOCKED_BY_ROBOTS_TXT");

        Assert.Equal(2, blocked.Finding.SampleUrls.Count);
    }

    [Fact]
    public void Poor_vitals_are_reported_per_metric()
    {
        var site = new CrawlSiteInput
        {
            SitemapFound = true,
            SitemapUrls = ["https://example.com/"],
            HomePageId = PageA,
            Vitals = new VitalsInput(LcpMs: 5200, Cls: 0.4, InpMs: 700)
        };

        var codes = CrawlRules.Evaluate([Home(PageA)], [], site)
            .Select(f => f.Finding.Code)
            .ToHashSet();

        Assert.Contains("LCP_POOR", codes);
        Assert.Contains("CLS_POOR", codes);
        Assert.Contains("INP_POOR", codes);
    }

    [Fact]
    public void Vitals_within_thresholds_or_missing_produce_nothing()
    {
        var good = new CrawlSiteInput
        {
            SitemapFound = true,
            SitemapUrls = ["https://example.com/"],
            HomePageId = PageA,
            Vitals = new VitalsInput(LcpMs: 2400, Cls: 0.05, InpMs: 180)
        };
        Assert.Empty(CrawlRules.Evaluate([Home(PageA)], [], good));

        var unknown = good with { Vitals = new VitalsInput(null, null, null) };
        Assert.Empty(CrawlRules.Evaluate([Home(PageA)], [], unknown));
    }
}
