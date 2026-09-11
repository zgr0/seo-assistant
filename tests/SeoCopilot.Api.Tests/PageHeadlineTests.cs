using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Gezinme etiketlerinin ("Hakkimizda", "Anasayfa") gonderiye ve gorsele baslik olarak
/// yazilmamasi. Etiket elenince sayfanin kendi anlatimina inilir.
/// </summary>
public class PageHeadlineTests
{
    private static readonly PlatformProfile Instagram = new()
    {
        Code = "instagram", DisplayName = "Instagram",
        MaxChars = 2200, RecommendedChars = 150, MaxHashtags = 5, SupportsLinks = false
    };

    [Theory]
    [InlineData("Hakkımızda")]
    [InlineData("hakkimizda")]
    [InlineData("Ana Sayfa")]
    [InlineData("İletişim")]
    [InlineData("Ürünler")]
    [InlineData("Kurumsal")]
    [InlineData("S.S.S")]
    [InlineData("About Us")]
    [InlineData("Hakkımızda.")]
    public void Navigation_labels_are_detected(string text) =>
        Assert.True(PageHeadline.IsNavigational(text));

    [Theory]
    [InlineData("YIZUMI Hakkında")]
    [InlineData("Plastik enjeksiyon makinaları")]
    [InlineData("Ürünlerimizde kullanılan servo teknolojisi")]
    public void Real_headlines_survive(string text) =>
        Assert.False(PageHeadline.IsNavigational(text));

    [Fact]
    public void Meaningful_falls_through_to_the_description()
    {
        var page = new Page
        {
            Url = "https://ornek.com/hakkimizda",
            H1Texts = ["Hakkımızda"],
            Title = "Hakkımızda | Örnek Makina",
            MetaDescription = "Plastik ve kauçuk enjeksiyon makinalarının satışı ve servisi."
        };

        Assert.Equal(
            "Plastik ve kauçuk enjeksiyon makinalarının satışı ve servisi",
            PageHeadline.Meaningful(page));
    }

    [Fact]
    public void Meaningful_is_null_when_only_labels_exist()
    {
        var page = new Page { Url = "https://ornek.com/iletisim", H1Texts = ["İletişim"], Title = "İletişim" };

        Assert.Null(PageHeadline.Meaningful(page));
    }

    [Fact]
    public void Post_body_does_not_open_with_a_navigation_label()
    {
        var page = new Page
        {
            Url = "https://ornek.com/hakkimizda",
            H1Texts = ["Hakkımızda"],
            MetaDescription = "20 yılı aşkın süredir enjeksiyon makinaları üretiyoruz."
        };

        var variant = PagePostBuilder.Build(page, Instagram, null, 0);

        Assert.DoesNotContain("Hakkımızda", variant.Body);
        Assert.StartsWith("20 yılı aşkın", variant.Body);
    }

    [Fact]
    public void Description_is_not_repeated_when_it_became_the_hook()
    {
        var page = new Page
        {
            Url = "https://ornek.com/hakkimizda",
            H1Texts = ["Hakkımızda"],
            MetaDescription = "20 yılı aşkın süredir enjeksiyon makinaları üretiyoruz."
        };

        var body = PagePostBuilder.Build(page, Instagram, null, 0).Body;

        // Ayni cumle hem kanca hem govde olarak iki kez gecmemeli.
        var occurrences = body.Split("20 yılı aşkın").Length - 1;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void Question_hook_is_skipped_for_long_headlines()
    {
        var page = new Page
        {
            Url = "https://ornek.com/hakkimizda",
            H1Texts = ["Hakkımızda"],
            MetaDescription = "Plastik, kauçuk ve metal enjeksiyon makinalarında satış ve teknik servis."
        };

        // merak_uyandiran aci (index 1) uzun basliga soru eki eklemez.
        Assert.DoesNotContain("nedir, ne işe yarar", PagePostBuilder.Build(page, Instagram, null, 1).Body);
    }

    [Fact]
    public void Image_prompt_skips_the_label_too()
    {
        var page = new Page
        {
            Url = "https://ornek.com/hakkimizda",
            H1Texts = ["Hakkımızda"],
            MetaDescription = "Plastik enjeksiyon makinaları üretimi ve servisi."
        };

        var brief = PageBriefBuilder.Build(page);

        Assert.DoesNotContain("Hakkımızda", brief);
        Assert.Contains("Plastik enjeksiyon", brief);
    }
}
