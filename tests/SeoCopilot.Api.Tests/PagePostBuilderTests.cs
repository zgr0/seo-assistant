using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Sablon tabanli gonderi uretimi — LLM yok. Model kapaliyken devreye giren yol.
/// </summary>
public class PagePostBuilderTests
{
    private static readonly PlatformProfile Instagram = new()
    {
        Code = "instagram", DisplayName = "Instagram",
        MaxChars = 2200, RecommendedChars = 150, MaxHashtags = 5, SupportsLinks = false
    };

    private static readonly PlatformProfile X = new()
    {
        Code = "x", DisplayName = "X",
        MaxChars = 280, RecommendedChars = 240, MaxHashtags = 2, SupportsLinks = true
    };

    private static Page SamplePage() => new()
    {
        Url = "https://ornek.com/enjeksiyon-makinalari",
        Title = "Enjeksiyon Makinaları | Örnek Makina | Anasayfa",
        H1Texts = ["Plastik enjeksiyon makinaları"],
        MetaDescription = "20 yılı aşkın süredir plastik enjeksiyon makinaları satış ve servisi.",
        MainText = string.Join(' ', Enumerable.Repeat("enjeksiyon", 6))
            + " " + string.Join(' ', Enumerable.Repeat("kalıplama", 4))
    };

    [Fact]
    public void Body_uses_the_heading_description_and_a_cta()
    {
        var variant = PagePostBuilder.Build(SamplePage(), Instagram, null, 0);

        Assert.StartsWith("Plastik enjeksiyon makinaları", variant.Body);
        Assert.Contains("20 yılı aşkın", variant.Body);
        Assert.Contains("bağlantı profilimizde", variant.Body);
        Assert.Equal(variant.Body.Length, variant.CharCount);
    }

    [Fact]
    public void Angle_rotates_with_the_post_index()
    {
        var page = SamplePage();

        Assert.Equal("bilgilendirici", PagePostBuilder.Build(page, Instagram, null, 0).Angle);
        Assert.Equal("merak_uyandiran", PagePostBuilder.Build(page, Instagram, null, 1).Angle);
        Assert.Equal("satis_odakli", PagePostBuilder.Build(page, Instagram, null, 2).Angle);
        Assert.Equal("bilgilendirici", PagePostBuilder.Build(page, Instagram, null, 3).Angle);
    }

    [Fact]
    public void Link_goes_into_the_cta_only_where_the_platform_supports_it()
    {
        var page = SamplePage();

        Assert.Contains(page.Url, PagePostBuilder.Build(page, X, null, 0).Cta);
        Assert.DoesNotContain(page.Url, PagePostBuilder.Build(page, Instagram, null, 0).Cta!);
    }

    [Fact]
    public void Body_respects_the_platform_character_limit()
    {
        var page = SamplePage();
        page.MetaDescription = new string('a', 500);

        var variant = PagePostBuilder.Build(page, X, null, 0);

        Assert.True(variant.Body.Length <= X.MaxChars, $"gövde {variant.Body.Length} karakter");
    }

    [Fact]
    public void Hashtags_are_ascii_capped_and_brand_defaults_come_first()
    {
        var brand = new BrandProfile { Name = "Marka", DefaultHashtags = ["#ornekmarka"] };

        var variant = PagePostBuilder.Build(SamplePage(), Instagram, brand, 0);

        Assert.Equal("#ornekmarka", variant.Hashtags[0]);
        Assert.True(variant.Hashtags.Count <= Instagram.MaxHashtags);
        Assert.All(variant.Hashtags, tag => Assert.StartsWith("#", tag));
        // Turkce harfler ASCII'ye cevrilir: 'kalıplama' -> '#kaliplama'
        Assert.All(variant.Hashtags, tag => Assert.DoesNotContain('ı', tag));
        Assert.Contains("#enjeksiyon", variant.Hashtags);
    }

    [Fact]
    public void Address_form_switches_the_cta_wording()
    {
        var page = SamplePage();
        var informal = new BrandProfile { Name = "Marka", AddressForm = AddressForm.Sen };
        var formal = new BrandProfile { Name = "Marka", AddressForm = AddressForm.Siz };

        Assert.Contains("göz at ", PagePostBuilder.Build(page, Instagram, informal, 0).Cta! + " ");
        Assert.Contains("göz atın", PagePostBuilder.Build(page, Instagram, formal, 0).Cta!);
    }

    [Fact]
    public void Title_is_trimmed_to_its_first_segment_when_there_is_no_h1()
    {
        var page = SamplePage();
        page.H1Texts = [];

        Assert.StartsWith("Enjeksiyon Makinaları", PagePostBuilder.Build(page, Instagram, null, 0).Body);
    }

    [Fact]
    public void Image_brief_and_alt_are_filled_from_the_page()
    {
        var variant = PagePostBuilder.Build(SamplePage(), Instagram, null, 0);

        Assert.Contains("no text", variant.ImageBrief!);
        Assert.Contains("Plastik enjeksiyon makinaları", variant.ImageAlt!);
    }
}
