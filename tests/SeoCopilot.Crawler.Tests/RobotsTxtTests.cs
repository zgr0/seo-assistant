namespace SeoCopilot.Crawler.Tests;

public class RobotsTxtTests
{
    [Fact]
    public void Allow_all_when_no_directives()
    {
        var robots = RobotsTxt.Parse("# bos dosya");
        Assert.True(robots.IsAllowed("/herhangi"));
        Assert.Empty(robots.Sitemaps);
    }

    [Fact]
    public void Disallow_applies_to_star_group()
    {
        var robots = RobotsTxt.Parse("""
            User-agent: *
            Disallow: /admin
            """);

        Assert.False(robots.IsAllowed("/admin/panel"));
        Assert.True(robots.IsAllowed("/blog"));
    }

    [Fact]
    public void Consecutive_user_agent_lines_form_one_group()
    {
        // "*" ikinci satirda; eski ayristirici son satiri kazandirdigi icin bunu kaciriyordu.
        var robots = RobotsTxt.Parse("""
            User-agent: Googlebot
            User-agent: *
            Disallow: /gizli
            """);

        Assert.False(robots.IsAllowed("/gizli/x"));
    }

    [Fact]
    public void Our_token_group_beats_the_star_group()
    {
        var robots = RobotsTxt.Parse("""
            User-agent: *
            Disallow: /

            User-agent: seocopilotbot
            Disallow: /admin
            """, "seocopilotbot");

        Assert.True(robots.IsAllowed("/blog"));
        Assert.False(robots.IsAllowed("/admin"));
    }

    [Fact]
    public void Longest_match_wins_and_allow_breaks_ties()
    {
        var robots = RobotsTxt.Parse("""
            User-agent: *
            Disallow: /shop
            Allow: /shop/kampanya
            """);

        Assert.False(robots.IsAllowed("/shop/urun"));
        Assert.True(robots.IsAllowed("/shop/kampanya/1"));
    }

    [Fact]
    public void Wildcard_and_end_anchor_are_supported()
    {
        var robots = RobotsTxt.Parse("""
            User-agent: *
            Disallow: /*.pdf$
            Disallow: /tmp/*/log
            """);

        Assert.False(robots.IsAllowed("/dosyalar/rapor.pdf"));
        Assert.True(robots.IsAllowed("/dosyalar/rapor.pdf.html"));
        Assert.False(robots.IsAllowed("/tmp/a/log"));
        Assert.True(robots.IsAllowed("/tmp/a/other"));
    }

    [Fact]
    public void Empty_disallow_means_no_restriction()
    {
        var robots = RobotsTxt.Parse("""
            User-agent: *
            Disallow:
            """);

        Assert.True(robots.IsAllowed("/herhangi"));
    }

    [Fact]
    public void Sitemaps_are_collected_regardless_of_group()
    {
        var robots = RobotsTxt.Parse("""
            Sitemap: https://example.com/sitemap.xml
            User-agent: *
            Disallow: /admin
            Sitemap: https://example.com/haber-sitemap.xml
            """);

        Assert.Equal(2, robots.Sitemaps.Count);
        Assert.Contains("https://example.com/sitemap.xml", robots.Sitemaps);
    }

    [Fact]
    public void Comments_are_stripped()
    {
        var robots = RobotsTxt.Parse("""
            User-agent: *   # herkes
            Disallow: /admin  # yonetim
            """);

        Assert.False(robots.IsAllowed("/admin"));
    }

    [Fact]
    public void Uri_overload_matches_path_and_query()
    {
        var robots = RobotsTxt.Parse("""
            User-agent: *
            Disallow: /ara?
            """);

        Assert.False(robots.IsAllowed(new Uri("https://example.com/ara?q=x")));
        Assert.True(robots.IsAllowed(new Uri("https://example.com/blog")));
    }
}
