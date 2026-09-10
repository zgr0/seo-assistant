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

    /// <param name="withImage">
    /// Sosyal paket uretimi — semaya aciklama ve gorsel brief alanlari eklenir.
    /// </param>
    public static string System(BrandProfile? brand, PlatformProfile? platform, bool withImage = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sen Türkçe çalışan bir SEO ve içerik editörüsün.");
        sb.AppendLine("Yalnız Türkçe yaz. Uydurma bilgi, fiyat veya istatistik ekleme.");
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
                sb.AppendLine($"- Kullanılmayacak ifadeler: {string.Join(", ", brand.BannedPhrases)}");
            if (brand.DefaultHashtags.Count > 0)
                sb.AppendLine($"- Varsayılan hashtag'ler: {string.Join(" ", brand.DefaultHashtags)}");
            if (brand.ExtraContext is { Length: > 0 }) sb.AppendLine($"- Ek bağlam: {brand.ExtraContext}");
            sb.AppendLine();
        }

        if (platform is not null)
        {
            sb.AppendLine($"# Platform: {platform.DisplayName}");
            sb.AppendLine($"- Azami {platform.MaxChars} karakter, önerilen {platform.RecommendedChars}.");
            sb.AppendLine($"- En fazla {platform.MaxHashtags} hashtag.");
            sb.AppendLine(platform.SupportsLinks
                ? "- Link paylaşımı desteklenir."
                : "- Gönderi metnine link koyma.");
            sb.AppendLine($"- {platform.GuidanceTr}");
            sb.AppendLine();
        }

        sb.AppendLine("# Çıktı biçimi");
        sb.AppendLine("Yalnız geçerli JSON döndür, kod bloğuna sarma, açıklama ekleme. Şema:");

        if (withImage)
        {
            sb.AppendLine("""{"variants":[{"angle":"bilgilendirici|merak_uyandiran|satis_odakli","body":"gönderi metni","description":"1-2 cümlelik kısa özet","hashtags":["#ornek"],"cta":"eylem çağrısı","imageBrief":"görsel sahnesi (İngilizce)","imageAlt":"görselin Türkçe alternatif metni"}]}""");
            sb.AppendLine();
            sb.AppendLine("imageBrief kuralları:");
            sb.AppendLine("- İngilizce yaz; bir görüntü üretim modeline verilecek.");
            sb.AppendLine("- Somut sahne tarif et: konu, ortam, ışık, kompozisyon, stil.");
            sb.AppendLine("- Görselin içine yazı, logo veya filigran isteme — model metni bozuk yazar.");
            sb.AppendLine("- Marka adını ya da gerçek kişileri tarif etme.");
            sb.AppendLine("imageAlt Türkçe olsun ve görseli betimlesin; body'yi tekrar etme.");
        }
        else
        {
            sb.AppendLine("""{"variants":[{"angle":"bilgilendirici|merak_uyandiran|satis_odakli","body":"metin","hashtags":["#ornek"],"cta":"eylem cagrisi"}]}""");
            sb.AppendLine("hashtags ve cta gerekmiyorsa boş bırak.");
        }

        return sb.ToString();
    }

    public static string User(ContentJob job, Page? page)
    {
        var input = ParseInput(job.Input);
        var count = VariantCount(input);

        var sb = new StringBuilder();
        sb.AppendLine($"# Görev ({count} farklı varyant üret)");
        sb.AppendLine(TaskText(job.Type));
        sb.AppendLine();

        if (page is not null)
        {
            sb.AppendLine("# Sayfa bağlamı");
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
            sb.AppendLine("# Girdi alanları (JSON)");
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
            "Sayfa için SEO uyumlu title etiketi yaz. 30-60 karakter, ana anahtar kelime başta, tıklama çekici.",
        ContentJobType.MetaDescription =>
            "Sayfa için meta description yaz. 120-155 karakter, özet + eylem çağrısı, anahtar kelimeyi doğal kullan.",
        ContentJobType.H1 =>
            "Sayfa için tek bir H1 başlığı yaz. Title'ı tekrar etme, sayfanın ana vaadini söyle.",
        ContentJobType.ProductDescription =>
            "Ürün açıklaması yaz. Fayda odaklı, taranabilir, özellikleri somut anlat; teknik veriyi uydurma.",
        ContentJobType.BlogOutline =>
            "Blog yazısı için plan çıkar. H2/H3 başlıkları ve her başlığın altında 1 cümlelik not ver.",
        ContentJobType.FixAdvice =>
            "Verilen SEO bulgusunu bu sayfaya özel hale getir. Gövdeyi üç bölüm halinde yaz:\n"
            + "1. Olası kök neden — kanıt alanındaki değere ve sayfa bağlamına dayanarak bu sayfada "
            + "sorunun neden çıktığını söyle.\n"
            + "2. Düzeltme adımları — numaralı ve somut. baseAdvice alanındaki genel metni "
            + "tekrarlama; onu bu sayfanın verisiyle özelleştir.\n"
            + "3. Nasıl doğrularım — değişiklikten sonra bakılacak tek bir somut kontrol.\n"
            + "Erişemediğin bilgiyi uydurma; emin olmadığın yerde neyin kontrol edilmesi gerektiğini "
            + "yaz. whenToIgnore alanındaki durum bu sayfa için geçerliyse bunu baştan belirt.",
        ContentJobType.SocialPost =>
            "Platforma uygun tek bir sosyal medya gönderisi yaz. İlk satır kanca olsun.",
        ContentJobType.SocialBatch =>
            "Platforma uygun, birbirinden farklı açılarda sosyal medya gönderileri yaz.",
        ContentJobType.SocialKit =>
            "Platforma uygun, birbirinden farklı açılarda örnek sosyal medya gönderileri yaz. " +
            "Her gönderi tek başına yayına hazır olsun: ilk satır kanca, ardından değer, sonunda eylem çağrısı. " +
            "Her gönderi için ayrıca kısa bir açıklama ve gönderiye eşlik edecek görselin brief'ini ver.",
        ContentJobType.HashtagSet =>
            "İçeriğe uygun hashtag seti öner. Genel + niche karışımı olsun, spam görünmesin.",
        _ => "Sayfa için istenen içeriği üret."
    };

    private static string ToneText(BrandTone tone) => tone switch
    {
        BrandTone.Kurumsal => "kurumsal, ölçülü",
        BrandTone.Samimi => "samimi, günlük dil",
        BrandTone.Teknik => "teknik, kesin",
        BrandTone.SatisOdakli => "satış odaklı, ikna edici",
        _ => "nötr"
    };

    private static string EmojiText(EmojiUsage usage) => usage switch
    {
        EmojiUsage.None => "kullanma",
        EmojiUsage.Light => "az sayıda, yerinde kullan",
        EmojiUsage.Heavy => "bol kullan",
        _ => "kullanma"
    };
}
