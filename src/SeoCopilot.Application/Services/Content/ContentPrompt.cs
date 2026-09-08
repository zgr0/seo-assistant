using System.Text;
using System.Text.Json;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services.Content;

/// <summary>
/// content_jobs satirini bir Anthropic istemine cevirir. Marka profili, platform kurallari
/// ve sayfa baglami sistem istemine; is turune ozgu gorev kullanici istemine yazilir.
/// Model her zaman ayni JSON kabugunu dondurmeye zorlanir (bkz. <see cref="ContentResponseParser"/>).
/// </summary>
public static class ContentPrompt
{
    /// <summary>Sayfa ana metninden isteme tasinan azami karakter.</summary>
    public const int MaxPageContextChars = 4000;

    public const int DefaultVariantCount = 3;
    public const int MaxVariantCount = 5;

    public static string System(BrandProfile? brand, PlatformProfile? platform)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sen Turkce calisan bir SEO ve icerik editorusun.");
        sb.AppendLine("Yalniz Turkce yaz. Uydurma bilgi, fiyat veya istatistik ekleme.");
        sb.AppendLine();

        if (brand is not null)
        {
            sb.AppendLine("# Marka sesi");
            sb.AppendLine($"- Profil: {brand.Name}");
            sb.AppendLine($"- Ton: {ToneText(brand.Tone)}");
            sb.AppendLine($"- Hitap: {(brand.AddressForm == AddressForm.Sen ? "sen dili" : "siz dili")}");
            sb.AppendLine($"- Emoji: {EmojiText(brand.EmojiUsage)}");
            if (brand.TargetAudience is { Length: > 0 }) sb.AppendLine($"- Hedef kitle: {brand.TargetAudience}");
            if (brand.BannedPhrases.Count > 0)
                sb.AppendLine($"- Kullanilmayacak ifadeler: {string.Join(", ", brand.BannedPhrases)}");
            if (brand.DefaultHashtags.Count > 0)
                sb.AppendLine($"- Varsayilan hashtag'ler: {string.Join(" ", brand.DefaultHashtags)}");
            if (brand.ExtraContext is { Length: > 0 }) sb.AppendLine($"- Ek baglam: {brand.ExtraContext}");
            sb.AppendLine();
        }

        if (platform is not null)
        {
            sb.AppendLine($"# Platform: {platform.DisplayName}");
            sb.AppendLine($"- Azami {platform.MaxChars} karakter, onerilen {platform.RecommendedChars}.");
            sb.AppendLine($"- En fazla {platform.MaxHashtags} hashtag.");
            sb.AppendLine(platform.SupportsLinks
                ? "- Link paylasimi desteklenir."
                : "- Gonderi metnine link koyma.");
            sb.AppendLine($"- {platform.GuidanceTr}");
            sb.AppendLine();
        }

        sb.AppendLine("# Cikti bicimi");
        sb.AppendLine("Yalniz gecerli JSON dondur, kod bloguna sarma, aciklama ekleme. Sema:");
        sb.AppendLine("""{"variants":[{"angle":"bilgilendirici|merak_uyandiran|satis_odakli","body":"metin","hashtags":["#ornek"],"cta":"eylem cagrisi"}]}""");
        sb.AppendLine("hashtags ve cta gerekmiyorsa bos birak.");

        return sb.ToString();
    }

    public static string User(ContentJob job, Page? page)
    {
        var input = ParseInput(job.Input);
        var count = VariantCount(input);

        var sb = new StringBuilder();
        sb.AppendLine($"# Gorev ({count} farkli varyant uret)");
        sb.AppendLine(TaskText(job.Type));
        sb.AppendLine();

        if (page is not null)
        {
            sb.AppendLine("# Sayfa baglami");
            sb.AppendLine($"- URL: {page.Url}");
            if (page.Title is { Length: > 0 }) sb.AppendLine($"- Mevcut title: {page.Title}");
            if (page.MetaDescription is { Length: > 0 })
                sb.AppendLine($"- Mevcut meta description: {page.MetaDescription}");
            if (page.H1Texts.Count > 0) sb.AppendLine($"- H1: {string.Join(" | ", page.H1Texts)}");
            if (page.MainText is { Length: > 0 })
            {
                var text = page.MainText.Length > MaxPageContextChars
                    ? page.MainText[..MaxPageContextChars]
                    : page.MainText;
                sb.AppendLine("- Sayfa metni:");
                sb.AppendLine(text);
            }
            sb.AppendLine();
        }

        if (input is not null && input.Value.EnumerateObject().Any())
        {
            sb.AppendLine("# Girdi alanlari (JSON)");
            sb.AppendLine(input.Value.GetRawText());
        }

        return sb.ToString();
    }

    public static int VariantCount(JsonElement? input) =>
        input is JsonElement el
            && el.ValueKind == JsonValueKind.Object
            && el.TryGetProperty("variantCount", out var value)
            && value.TryGetInt32(out var count)
            ? Math.Clamp(count, 1, MaxVariantCount)
            : DefaultVariantCount;

    public static JsonElement? ParseInput(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object ? doc.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string TaskText(ContentJobType type) => type switch
    {
        ContentJobType.Title =>
            "Sayfa icin SEO uyumlu title etiketi yaz. 30-60 karakter, ana anahtar kelime basta, tiklama cekici.",
        ContentJobType.MetaDescription =>
            "Sayfa icin meta description yaz. 120-155 karakter, ozet + eylem cagrisi, anahtar kelimeyi dogal kullan.",
        ContentJobType.H1 =>
            "Sayfa icin tek bir H1 basligi yaz. Title'i tekrar etme, sayfanin ana vaadini soyle.",
        ContentJobType.ProductDescription =>
            "Urun aciklamasi yaz. Fayda odakli, taranabilir, ozellikleri somut anlat; teknik veriyi uydurma.",
        ContentJobType.BlogOutline =>
            "Blog yazisi icin plan cikar. H2/H3 basliklari ve her basligin altinda 1 cumlelik not ver.",
        ContentJobType.FixAdvice =>
            "Verilen SEO bulgusunu bu sayfaya ozel hale getir. Govdeyi uc bolum halinde yaz:\n"
            + "1. Olasi kok neden — kanit alanindaki degere ve sayfa baglamina dayanarak bu sayfada "
            + "sorunun neden ciktigini soyle.\n"
            + "2. Duzeltme adimlari — numarali ve somut. baseAdvice alanindaki genel metni "
            + "tekrarlama; onu bu sayfanin verisiyle ozellestir.\n"
            + "3. Nasil dogrularim — degisiklikten sonra bakilacak tek bir somut kontrol.\n"
            + "Erisemedigin bilgiyi uydurma; emin olmadigin yerde neyin kontrol edilmesi gerektigini "
            + "yaz. whenToIgnore alanindaki durum bu sayfa icin gecerliyse bunu bastan belirt.",
        ContentJobType.SocialPost =>
            "Platforma uygun tek bir sosyal medya gonderisi yaz. Ilk satir kanca olsun.",
        ContentJobType.SocialBatch =>
            "Platforma uygun, birbirinden farkli acilarda sosyal medya gonderileri yaz.",
        ContentJobType.HashtagSet =>
            "Icerige uygun hashtag seti oner. Genel + niche karisimi olsun, spam gorunmesin.",
        _ => "Sayfa icin istenen icerigi uret."
    };

    private static string ToneText(BrandTone tone) => tone switch
    {
        BrandTone.Kurumsal => "kurumsal, olculu",
        BrandTone.Samimi => "samimi, gunluk dil",
        BrandTone.Teknik => "teknik, kesin",
        BrandTone.SatisOdakli => "satis odakli, ikna edici",
        _ => "notr"
    };

    private static string EmojiText(EmojiUsage usage) => usage switch
    {
        EmojiUsage.None => "kullanma",
        EmojiUsage.Light => "az sayida, yerinde kullan",
        EmojiUsage.Heavy => "bol kullan",
        _ => "kullanma"
    };
}
