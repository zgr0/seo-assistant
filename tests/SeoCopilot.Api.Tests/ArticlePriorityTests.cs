using SeoCopilot.Application.Services.Content;
using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Haber/blog yazilarinin sosyal gonderi kaynagi olarak one alinmasi: sinif tespiti,
/// siralama, liste sayfalarinin geri itilmesi ve yaziya uygun sablon.
/// </summary>
public class ArticlePriorityTests
{
    private static readonly PlatformProfile Instagram = new()
    {
        Code = "instagram", DisplayName = "Instagram",
        MaxChars = 2200, RecommendedChars = 150, MaxHashtags = 5, SupportsLinks = false
    };

    private static Page NewPage(
        string url, int depth = 1, int inlinks = 10, int words = 400, string? og = null,
        List<string>? schema = null) => new()
    {
        Url = url,
        StatusCode = 200,
        Depth = depth,
        InlinkCount = inlinks,
        WordCount = words,
        Title = "Başlık",
        H1Texts = ["Servo motorlu enjeksiyon makinalarında enerji tasarrufu"],
        MetaDescription = "Yeni nesil servo sistemler enerji tüketimini belirgin biçimde azaltıyor.",
        MainText = new string('a', 800),
        OgData = og,
        SchemaTypes = schema ?? []
    };

    // --- sinif tespiti ---

    [Theory]
    [InlineData("https://ornek.com/blog/servo-motor-rehberi")]
    [InlineData("https://ornek.com/haberler/fuar-2026")]
    [InlineData("https://ornek.com/tr/haber/yeni-bayi")]
    [InlineData("https://ornek.com/news/new-plant")]
    [InlineData("https://ornek.com/duyurular/tatil-calisma-saatleri")]
    [InlineData("https://ornek.com/2025/04/yeni-urun-tanitimi")]
    [InlineData("https://ornek.com/2025/04/18/yeni-urun-tanitimi")]
    // generalmakina.com.tr gercek yapisi: liste /blog, yazilar /blogyazisi/<slug>
    [InlineData("https://www.generalmakina.com.tr/blogyazisi/ankiros-2024-fuarini-basariyla-tamamladik")]
    [InlineData("https://ornek.com/blog-yazilari/servo")]
    [InlineData("https://ornek.com/haberdetay/fuar")]
    [InlineData("https://ornek.com/news-detail/new-plant")]
    public void Article_urls_are_detected(string url) =>
        Assert.Equal(PageKind.Article, PageClassifier.Classify(NewPage(url)));

    [Theory]
    [InlineData("https://ornek.com/urunler/haberlesme-sistemleri")]  // 'haber' ile baslayan urun
    [InlineData("https://ornek.com/basincli-dokum-akilli-uniteleri")] // 'basin' ile baslayan urun
    [InlineData("https://ornek.com/blogger-araclari")]
    public void Words_that_merely_start_like_a_section_are_not_articles(string url) =>
        Assert.Equal(PageKind.Other, PageClassifier.Classify(NewPage(url)));

    [Theory]
    [InlineData("https://ornek.com/blog")]
    [InlineData("https://ornek.com/haberler/")]
    [InlineData("https://ornek.com/blog/sayfa/2")]
    [InlineData("https://ornek.com/haberler/3")]
    public void Section_indexes_are_listings(string url) =>
        Assert.Equal(PageKind.Listing, PageClassifier.Classify(NewPage(url)));

    [Theory]
    [InlineData("https://ornek.com/")]
    [InlineData("https://ornek.com/hakkimizda")]
    [InlineData("https://ornek.com/urunler/servo-a6")]
    [InlineData("https://ornek.com/2025/04")]           // arsiv listesi, yazi degil
    public void Other_pages_are_not_articles(string url) =>
        Assert.Equal(PageKind.Other, PageClassifier.Classify(NewPage(url)));

    [Fact]
    public void Structured_data_and_og_type_mark_articles_regardless_of_the_url()
    {
        Assert.Equal(PageKind.Article,
            PageClassifier.Classify(NewPage("https://ornek.com/servo-rehberi", schema: ["BlogPosting"])));

        Assert.Equal(PageKind.Article,
            PageClassifier.Classify(NewPage("https://ornek.com/servo-rehberi", og: """{"og:type":"article"}""")));
    }

    [Fact]
    public void Site_wide_og_article_is_ignored_and_the_url_decides()
    {
        // generalmakina.com.tr gibi: her sayfada og:type=article, sema yok.
        const string og = """{"og:type":"article"}""";
        var pages = new List<Page>
        {
            NewPage("https://ornek.com/", depth: 0, inlinks: 200, og: og),
            NewPage("https://ornek.com/hakkimizda", inlinks: 150, og: og),
            NewPage("https://ornek.com/otomotiv", inlinks: 120, og: og),
            NewPage("https://ornek.com/teknik-servis", inlinks: 110, og: og),
            NewPage("https://ornek.com/blogyazisi/ankiros-2024", depth: 1, inlinks: 2, og: og)
        };

        var selected = PageSelector.SelectWithKinds(pages, 5);

        // Isaret site geneli oldugu icin yok sayilir: tek yazi URL'den taninan.
        Assert.Equal("https://ornek.com/blogyazisi/ankiros-2024", selected[0].Page.Url);
        Assert.Equal(PageKind.Article, selected[0].Kind);
        Assert.All(selected.Skip(1), s => Assert.Equal(PageKind.Other, s.Kind));
    }

    [Fact]
    public void Markup_is_trusted_when_only_some_pages_carry_it()
    {
        var pages = new List<Page>
        {
            NewPage("https://ornek.com/", depth: 0, inlinks: 200),
            NewPage("https://ornek.com/hakkimizda", inlinks: 150),
            NewPage("https://ornek.com/otomotiv", inlinks: 120),
            NewPage("https://ornek.com/servo-rehberi", inlinks: 2, schema: ["BlogPosting"])
        };

        var top = PageSelector.SelectWithKinds(pages, 1)[0];

        Assert.Equal("https://ornek.com/servo-rehberi", top.Page.Url);
        Assert.Equal(PageKind.Article, top.Kind);
    }

    [Fact]
    public void Kind_from_the_selector_overrides_page_markup_in_the_template()
    {
        // Sayfa og:type=article tasiyor ama secici "other" dedi — sablon satis acisini kullanabilmeli.
        var page = NewPage("https://ornek.com/otomotiv", og: """{"og:type":"article"}""");

        var variant = PagePostBuilder.Build(page, Instagram, null, 2, PageKind.Other);

        Assert.Equal("satis_odakli", variant.Angle);
    }

    [Fact]
    public void Prompt_uses_the_stored_page_kind()
    {
        var page = NewPage("https://ornek.com/otomotiv", og: """{"og:type":"article"}""");
        var job = new ContentJob { Type = ContentJobType.SocialKit, Input = """{"pageKind":"other"}""" };

        Assert.DoesNotContain("haber/blog yazısı", ContentPrompt.User(job, page));
    }

    // --- siralama ---

    [Fact]
    public void Articles_come_before_the_home_page_and_heavily_linked_pages()
    {
        var pages = new List<Page>
        {
            NewPage("https://ornek.com/", depth: 0, inlinks: 200),
            NewPage("https://ornek.com/hakkimizda", depth: 1, inlinks: 150),
            NewPage("https://ornek.com/blog/servo-rehberi", depth: 2, inlinks: 3),
            NewPage("https://ornek.com/haberler/fuar-2026", depth: 2, inlinks: 2)
        };

        var selected = PageSelector.Select(pages, 3).Select(p => p.Url).ToList();

        Assert.Equal("https://ornek.com/blog/servo-rehberi", selected[0]);
        Assert.Equal("https://ornek.com/haberler/fuar-2026", selected[1]);
        // Yazilar tukenince diger sayfalara donulur.
        Assert.Equal("https://ornek.com/", selected[2]);
    }

    [Fact]
    public void Longer_articles_win_among_articles()
    {
        var pages = new List<Page>
        {
            NewPage("https://ornek.com/blog/kisa", words: 300),
            NewPage("https://ornek.com/blog/uzun", words: 1800)
        };

        Assert.Equal("https://ornek.com/blog/uzun", PageSelector.Select(pages, 1)[0].Url);
    }

    [Fact]
    public void Listing_pages_are_pushed_behind_ordinary_pages()
    {
        var pages = new List<Page>
        {
            NewPage("https://ornek.com/blog", depth: 1, inlinks: 120),
            NewPage("https://ornek.com/urunler/servo-a6", depth: 2, inlinks: 5)
        };

        Assert.Equal("https://ornek.com/urunler/servo-a6", PageSelector.Select(pages, 1)[0].Url);
    }

    [Theory]
    [InlineData("https://ornek.com/haber/karaman-fuari")]        // 'arama' alt dizesi
    [InlineData("https://ornek.com/blog/girisimcilik-rehberi")]  // 'giris' oneki
    [InlineData("https://ornek.com/urunler/etiketleme-makinasi")] // 'etiket' oneki
    [InlineData("https://ornek.com/blog/heritage-koleksiyonu")]  // 'tag' alt dizesi
    public void Real_content_is_not_mistaken_for_boilerplate(string url) =>
        Assert.Single(PageSelector.Select([NewPage(url)], 1));

    [Theory]
    [InlineData("https://ornek.com/kisisel-verilerin-korunmasi")]
    [InlineData("https://ornek.com/kvkk-aydinlatma-metni")]
    [InlineData("https://ornek.com/gizlilik-politikasi")]
    [InlineData("https://ornek.com/blog/etiket/servo")]
    [InlineData("https://ornek.com/arama")]
    public void Legal_and_functional_pages_are_still_skipped(string url) =>
        Assert.Empty(PageSelector.Select([NewPage(url)], 1));

    // --- sablon ---

    [Fact]
    public void Article_posts_never_use_the_sales_angle_or_a_quote_cta()
    {
        var page = NewPage("https://ornek.com/blog/servo-rehberi");

        for (var index = 0; index < 6; index++)
        {
            var variant = PagePostBuilder.Build(page, Instagram, null, index);

            Assert.NotEqual("satis_odakli", variant.Angle);
            Assert.DoesNotContain("Teklif", variant.Cta!);
        }

        Assert.Contains("Yazının tamamını", PagePostBuilder.Build(page, Instagram, null, 0).Cta!);
    }

    [Fact]
    public void Ordinary_pages_keep_the_three_angle_rotation()
    {
        var page = NewPage("https://ornek.com/urunler/servo-a6");

        Assert.Equal("satis_odakli", PagePostBuilder.Build(page, Instagram, null, 2).Angle);
    }

    [Fact]
    public void Model_prompt_marks_articles()
    {
        var article = NewPage("https://ornek.com/blog/servo-rehberi");
        var product = NewPage("https://ornek.com/urunler/servo-a6");
        var job = new ContentJob { Type = ContentJobType.SocialKit, Input = "{}" };

        Assert.Contains("haber/blog yazısı", ContentPrompt.User(job, article));
        Assert.DoesNotContain("haber/blog yazısı", ContentPrompt.User(job, product));
    }
}
