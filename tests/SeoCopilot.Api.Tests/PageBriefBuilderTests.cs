using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Crawling;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Tarama verisinden gorsel istemi uretimi — LLM cagrisi yok, tamamen deterministik.
/// </summary>
public class PageBriefBuilderTests
{
    [Fact]
    public void Heading_and_description_carry_the_subject()
    {
        var page = new Page
        {
            Url = "https://ornek.com/kahve",
            Title = "Kahve çekirdekleri",
            H1Texts = ["Taze kavrulmuş kahve çekirdekleri"],
            MetaDescription = "Haftalık kavrulan tek kaynak kahve çekirdekleri kapınıza gelsin."
        };

        var brief = PageBriefBuilder.Build(page);

        Assert.Contains("Taze kavrulmuş kahve çekirdekleri", brief);
        Assert.Contains("Haftalık kavrulan", brief);
        Assert.Contains("no text", brief);
        Assert.Contains("no logo", brief);
    }

    [Fact]
    public void H1_wins_over_title()
    {
        var page = new Page
        {
            Url = "https://ornek.com",
            Title = "Ana sayfa | Örnek Mağaza",
            H1Texts = ["El yapımı seramik kupalar"]
        };

        Assert.StartsWith("El yapımı seramik kupalar", PageBriefBuilder.Build(page));
    }

    [Fact]
    public void Open_graph_fills_in_when_meta_tags_are_missing()
    {
        var page = new Page
        {
            Url = "https://ornek.com/blog",
            OgData = """{"og:title":"Bisiklet bakımı rehberi","og:description":"Zincir temizliği adım adım."}"""
        };

        var brief = PageBriefBuilder.Build(page);

        Assert.Contains("Bisiklet bakımı rehberi", brief);
        Assert.Contains("Zincir temizliği", brief);
    }

    [Fact]
    public void Frequent_words_become_keywords_and_stop_words_are_skipped()
    {
        var page = new Page
        {
            Url = "https://ornek.com/urun",
            H1Texts = ["Ürün"],
            MainText = string.Join(' ', Enumerable.Repeat("bisiklet", 5))
                + " " + string.Join(' ', Enumerable.Repeat("için", 9))
                + " " + string.Join(' ', Enumerable.Repeat("kask", 4))
        };

        var brief = PageBriefBuilder.Build(page);

        Assert.Contains("bisiklet", brief);
        Assert.Contains("kask", brief);
        Assert.DoesNotContain("için,", brief);
    }

    [Fact]
    public void Empty_page_falls_back_to_a_neutral_scene()
    {
        var brief = PageBriefBuilder.Build(new Page { Url = "https://ornek.com/bos" });

        Assert.Contains("modern workspace", brief);
        Assert.Contains("no text", brief);
    }

    [Fact]
    public void Long_subjects_are_clipped()
    {
        var page = new Page
        {
            Url = "https://ornek.com/uzun",
            H1Texts = [new string('a', 400)],
            MetaDescription = new string('b', 400)
        };

        var brief = PageBriefBuilder.Build(page);

        Assert.DoesNotContain(new string('b', 200), brief);
        Assert.True(brief.Length < PageBriefBuilder.MaxSubjectChars + 300);
    }

    [Fact]
    public void Alt_text_describes_the_heading()
    {
        var page = new Page { Url = "https://ornek.com", H1Texts = ["Kahve çekirdekleri"] };

        Assert.Equal("Kahve çekirdekleri konusunu temsil eden görsel", PageBriefBuilder.BuildAlt(page));
    }
}
