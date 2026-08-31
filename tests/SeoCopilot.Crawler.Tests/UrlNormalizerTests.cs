using SeoCopilot.Application.Common;

namespace SeoCopilot.Crawler.Tests;

public class UrlNormalizerTests
{
    private static readonly Uri Base = new("https://example.com/blog/");

    [Theory]
    [InlineData("/hakkinda", "https://example.com/hakkinda")]
    [InlineData("yazi-1", "https://example.com/blog/yazi-1")]
    [InlineData("../iletisim", "https://example.com/iletisim")]
    [InlineData("https://example.com/x", "https://example.com/x")]
    [InlineData("//example.com/y", "https://example.com/y")]
    public void Resolves_relative_and_absolute_hrefs(string href, string expected)
    {
        Assert.True(UrlNormalizer.TryNormalize(href, Base, out var url));
        Assert.Equal(expected, url.AbsoluteUri);
    }

    [Theory]
    [InlineData("#bolum")]
    [InlineData("mailto:a@b.com")]
    [InlineData("tel:+900000")]
    [InlineData("javascript:void(0)")]
    [InlineData("data:text/plain,x")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_non_http_targets(string? href)
    {
        Assert.False(UrlNormalizer.TryNormalize(href, Base, out _));
    }

    [Fact]
    public void Drops_fragment()
    {
        Assert.True(UrlNormalizer.TryNormalize("/a#bolum", Base, out var url));
        Assert.Equal("https://example.com/a", url.AbsoluteUri);
    }

    [Fact]
    public void Drops_tracking_params_but_keeps_the_rest()
    {
        Assert.True(UrlNormalizer.TryNormalize("/a?utm_source=x&id=7&fbclid=z&q=ara", Base, out var url));
        Assert.Equal("https://example.com/a?id=7&q=ara", url.AbsoluteUri);
    }

    [Fact]
    public void Drops_query_entirely_when_only_tracking_params_remain()
    {
        Assert.True(UrlNormalizer.TryNormalize("/a?utm_source=x&gclid=y", Base, out var url));
        Assert.Equal("https://example.com/a", url.AbsoluteUri);
    }

    [Fact]
    public void Lowercases_scheme_and_host_but_not_path()
    {
        Assert.True(UrlNormalizer.TryNormalize("HTTPS://Example.COM/Yazi", Base, out var url));
        Assert.Equal("https://example.com/Yazi", url.AbsoluteUri);
    }

    [Fact]
    public void Drops_default_port_and_keeps_custom_one()
    {
        Assert.True(UrlNormalizer.TryNormalize("https://example.com:443/a", Base, out var standard));
        Assert.Equal("https://example.com/a", standard.AbsoluteUri);

        Assert.True(UrlNormalizer.TryNormalize("http://example.com:8080/a", Base, out var custom));
        Assert.Equal("http://example.com:8080/a", custom.AbsoluteUri);
    }

    [Fact]
    public void Empty_path_becomes_root()
    {
        Assert.True(UrlNormalizer.TryNormalize("https://example.com", Base, out var url));
        Assert.Equal("https://example.com/", url.AbsoluteUri);
    }

    [Theory]
    [InlineData("https://example.com/a", true)]
    [InlineData("https://www.example.com/a", true)]
    [InlineData("http://example.com/a", true)]
    [InlineData("https://other.com/a", false)]
    [InlineData("https://sub.example.com/a", false)]
    public void Internal_check_ignores_www_and_scheme(string url, bool expected)
    {
        Assert.Equal(expected, UrlNormalizer.IsInternal(new Uri(url), Base));
    }

    [Theory]
    [InlineData("https://Example.com/", "https://example.com")]
    [InlineData("https://example.com", "https://example.com")]
    [InlineData("https://example.com/shop/", "https://example.com/shop")]
    [InlineData("http://example.com:8080/", "http://example.com:8080")]
    public void Site_base_url_is_a_trimmed_root(string input, string expected)
    {
        Assert.Equal(expected, UrlNormalizer.NormalizeSiteBaseUrl(input));
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("")]
    [InlineData(null)]
    public void Invalid_site_base_url_is_null(string? input)
    {
        Assert.Null(UrlNormalizer.NormalizeSiteBaseUrl(input));
    }

    [Fact]
    public void Hash_is_stable_and_url_specific()
    {
        Assert.Equal(
            UrlNormalizer.Hash("https://example.com/a"),
            UrlNormalizer.Hash(new Uri("https://example.com/a")));

        Assert.NotEqual(
            UrlNormalizer.Hash("https://example.com/a"),
            UrlNormalizer.Hash("https://example.com/b"));
    }
}
