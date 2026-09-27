using Sitecraft.Application.Services.Content;
using Sitecraft.Application.Services.Social;
using Sitecraft.Domain.Entities.Content;
using Sitecraft.Domain.Entities.Crawling;
using Sitecraft.Domain.Enums;

namespace Sitecraft.Api.Tests;

/// <summary>
/// Marka sesinin metne yansimasi: uretim sonrasi denetim (<see cref="BrandGuard"/>), sistem
/// istemi ve sablon yolundaki ton, emoji, hesap adi ve hashtag davranisi. Hepsi yerel.
/// </summary>
public class BrandVoiceTests
{
    private static readonly PlatformProfile Instagram = new()
    {
        Code = "instagram", DisplayName = "Instagram",
        MaxChars = 2200, RecommendedChars = 150, MaxHashtags = 5, SupportsLinks = false,
        GuidanceTr = "Görsel odaklı."
    };

    private static readonly PlatformProfile LinkedIn = new()
    {
        Code = "linkedin", DisplayName = "LinkedIn",
        MaxChars = 3000, RecommendedChars = 1300, MaxHashtags = 5, SupportsLinks = true,
        GuidanceTr = "Profesyonel."
    };

    private static Page SamplePage() => new()
    {
        Url = "https://ornek.com/enjeksiyon-makinalari",
        H1Texts = ["Plastik enjeksiyon makinaları"],
        MetaDescription = "20 yılı aşkın süredir plastik enjeksiyon makinaları satış ve servisi.",
        MainText = "Enjeksiyon makinalarımız en ucuz fiyat garantisiyle sunulur ve servis ağımız geniştir. " +
            "Servo motorlu makinalar enerji tüketimini belirgin biçimde azaltır ve sessiz çalışır. " +
            string.Join(' ', Enumerable.Repeat("enjeksiyon", 6))
    };

    private static BrandProfile Brand(Action<BrandProfile>? configure = null)
    {
        var brand = new BrandProfile { Name = "Örnek Makina" };
        configure?.Invoke(brand);
        return brand;
    }

    // --- BrandGuard ---

    [Theory]
    [InlineData("EN UCUZ fiyatlar burada", "en ucuz")]
    [InlineData("Büyük İNDİRİM başladı", "indirim")]
    [InlineData("En   ucuz\nürünler", "en ucuz")]
    [InlineData("Ucuzluk kampanyası", "ucuz")]
    public void Banned_phrases_match_turkish_case_and_spacing(string text, string phrase)
    {
        var brand = Brand(b => b.BannedPhrases = [phrase]);

        Assert.Equal(phrase, BrandGuard.FindBanned(text, brand));
    }

    [Fact]
    public void Clean_text_or_no_profile_passes()
    {
        var brand = Brand(b => b.BannedPhrases = ["en ucuz"]);

        Assert.Null(BrandGuard.FindBanned("Uygun fiyatlı makinalar", brand));
        Assert.Null(BrandGuard.FindBanned("EN UCUZ", null));
    }

    [Fact]
    public void Variant_with_a_banned_phrase_is_rejected_and_banned_hashtags_are_dropped()
    {
        var brand = Brand(b => b.BannedPhrases = ["en ucuz"]);

        var dirty = new ContentVariant { Body = "Piyasanın en ucuz makinası", Hashtags = [] };
        var viaCta = new ContentVariant { Body = "Temiz metin", Cta = "En ucuz teklifi alın", Hashtags = [] };
        var clean = new ContentVariant { Body = "Servo motorlu makinalar", Hashtags = ["#EnUcuz", "#servo"] };

        Assert.False(BrandGuard.Apply(dirty, brand));
        Assert.False(BrandGuard.Apply(viaCta, brand));
        Assert.True(BrandGuard.Apply(clean, brand));
        Assert.Equal(["#servo"], clean.Hashtags);
    }

    [Fact]
    public void Emoji_is_stripped_only_when_the_brand_wants_none()
    {
        var none = Brand(b => b.EmojiUsage = EmojiUsage.None);
        var light = Brand(b => b.EmojiUsage = EmojiUsage.Light);

        var stripped = new ContentVariant { Body = "🚀 Yeni seri  geldi! 👍🏽\nŞimdi inceleyin ✅", Cta = "👉", Hashtags = [] };
        Assert.True(BrandGuard.Apply(stripped, none));
        Assert.Equal("Yeni seri geldi!\nŞimdi inceleyin", stripped.Body);
        Assert.Null(stripped.Cta);
        Assert.Equal(stripped.Body.Length, stripped.CharCount);

        var kept = new ContentVariant { Body = "🚀 Yeni seri geldi", Hashtags = [] };
        Assert.True(BrandGuard.Apply(kept, light));
        Assert.StartsWith("🚀", kept.Body);

        // Yalniz emojiden olusan govde bos kalir — varyant kullanilmaz.
        Assert.False(BrandGuard.Apply(new ContentVariant { Body = "🎉🎉", Hashtags = [] }, none));
    }

    // --- sistem istemi ---

    [Fact]
    public void Prompt_names_the_platform_handle_and_the_banned_phrases()
    {
        var brand = Brand(b =>
        {
            b.BannedPhrases = ["en ucuz"];
            b.SocialHandles = new() { ["instagram"] = "ornekmakina", ["linkedin"] = "ornek-makina" };
            b.ExtraContext = "1998'den beri makina üretiyoruz.";
        });

        var instagram = ContentPrompt.System(brand, Instagram, withImage: true);
        Assert.Contains("@ornekmakina", instagram);
        Assert.Contains("eylem çağrısında bu hesaba yönlendir", instagram);
        Assert.DoesNotContain("@ornek-makina", instagram);
        Assert.Contains("hashtag dahil hiçbir yerde geçmesin): en ucuz", instagram);
        Assert.Contains("Marka tanıtımı ve ek bağlam: 1998", instagram);

        // Platformsuz is: butun hesaplar listelenir.
        var general = ContentPrompt.System(brand, null);
        Assert.Contains("instagram @ornekmakina, linkedin @ornek-makina", general);
    }

    // --- sablon yolu ---

    [Fact]
    public void Brand_hashtags_keep_their_spelling_and_short_tags()
    {
        var brand = Brand(b => b.DefaultHashtags = ["#ai", "#ÖrnekMakina"]);

        var tags = PagePostBuilder.Build(SamplePage(), Instagram, brand, 0).Hashtags;

        Assert.Equal("#ai", tags[0]);
        Assert.Equal("#ÖrnekMakina", tags[1]);
    }

    [Fact]
    public void Banned_words_do_not_become_hashtags()
    {
        var brand = Brand(b => b.BannedPhrases = ["enjeksiyon"]);

        var tags = PagePostBuilder.Build(SamplePage(), Instagram, brand, 0).Hashtags;

        Assert.DoesNotContain(tags, t => t.Contains("enjeksiyon", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Emoji_setting_decorates_the_hook_and_cta()
    {
        var page = SamplePage();

        var none = PagePostBuilder.Build(page, Instagram, Brand(b => b.EmojiUsage = EmojiUsage.None), 0);
        var light = PagePostBuilder.Build(page, Instagram, Brand(b => b.EmojiUsage = EmojiUsage.Light), 0);
        var heavy = PagePostBuilder.Build(page, Instagram, Brand(b => b.EmojiUsage = EmojiUsage.Heavy), 0);

        Assert.Equal(none.Body, BrandGuard.StripEmoji(none.Body));
        Assert.StartsWith("📌 Plastik enjeksiyon", light.Body);
        Assert.DoesNotContain("👉", light.Body);
        Assert.StartsWith("📌 ", heavy.Body);
        Assert.StartsWith("👉 ", heavy.Cta);
    }

    [Fact]
    public void Sales_tone_opens_with_the_sales_angle()
    {
        var sales = Brand(b => b.Tone = BrandTone.SatisOdakli);

        Assert.Equal("satis_odakli", PagePostBuilder.Build(SamplePage(), Instagram, sales, 0).Angle);
        Assert.Equal("bilgilendirici", PagePostBuilder.Build(SamplePage(), Instagram, Brand(), 0).Angle);
    }

    [Fact]
    public void Corporate_and_technical_tones_skip_casual_question_hooks()
    {
        var page = SamplePage();

        // Merak acisi: index 1 (tur 0), 4 (tur 1), 7 (tur 2).
        var casualHooks = new[] { 4, 7 }
            .Select(i => PagePostBuilder.Build(page, Instagram, Brand(b => b.Tone = BrandTone.Samimi), i).Body)
            .ToList();
        Assert.Contains(casualHooks, body => body.Contains("işin aslı ne?"));
        Assert.Contains(casualHooks, body => body.Contains("Hiç düşündünüz mü?"));

        foreach (var tone in new[] { BrandTone.Kurumsal, BrandTone.Teknik })
        {
            var bodies = new[] { 1, 4, 7 }
                .Select(i => PagePostBuilder.Build(page, Instagram, Brand(b => b.Tone = tone), i).Body)
                .ToList();

            Assert.DoesNotContain(bodies, body => body.Contains("işin aslı ne?"));
            Assert.DoesNotContain(bodies, body => body.Contains("Hiç düşündün"));
            // Kaliplar yine birbirinden farkli.
            Assert.Equal(3, bodies.Distinct().Count());
        }
    }

    [Fact]
    public void Linkless_platform_cta_points_to_the_brand_handle()
    {
        var brand = Brand(b => b.SocialHandles = new() { ["instagram"] = "ornekmakina" });

        Assert.EndsWith("bağlantı @ornekmakina profilinde.", PagePostBuilder.Build(SamplePage(), Instagram, brand, 0).Cta);
        Assert.EndsWith("bağlantı profilimizde.", PagePostBuilder.Build(SamplePage(), Instagram, Brand(), 0).Cta);

        // Link destekleyen platformda adres kalir, hesap adi eklenmez.
        var linkedIn = PagePostBuilder.Build(SamplePage(), LinkedIn, brand, 0).Cta!;
        Assert.Contains("https://ornek.com/enjeksiyon-makinalari", linkedIn);
    }
}
