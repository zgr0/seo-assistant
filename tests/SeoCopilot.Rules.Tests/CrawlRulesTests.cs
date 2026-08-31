using System.Text;

namespace SeoCopilot.Rules.Tests;

public class CrawlRulesTests
{
    private static readonly Guid PageA = Guid.CreateVersion7();
    private static readonly Guid PageB = Guid.CreateVersion7();
    private static readonly Guid PageC = Guid.CreateVersion7();

    private static byte[] Hash(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Identical_content_hashes_produce_one_duplicate_finding()
    {
        CrawlPageInput[] pages =
        [
            new(PageA, "https://example.com/a", 200, Hash("same")),
            new(PageB, "https://example.com/b", 200, Hash("same")),
            new(PageC, "https://example.com/c", 200, Hash("other")),
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
}
