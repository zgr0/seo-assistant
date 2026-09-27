using System.Globalization;
using System.Text;
using Sitecraft.Domain.Entities.Content;
using Sitecraft.Domain.Enums;

namespace Sitecraft.Application.Services.Content;

/// <summary>
/// Marka kurallarinin uretim sonrasi denetimi. Istem modele yasakli ifadeleri ve emoji tercihini
/// soyler ama uyacagi garanti degildir; sablon da sayfa metnini aynen tasir. Yasakli ifade iceren
/// metin reddedilir, yasakli hashtag ayiklanir, emoji istenmiyorsa silinir.
/// </summary>
public static class BrandGuard
{
    /// <summary>Turkce buyuk/kucuk harf: 'İNDİRİM' = 'indirim', 'IŞIK' = 'ışık'.</summary>
    private static readonly CompareInfo Turkish = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    /// <summary>
    /// Metinde gecen ilk yasakli ifade; yoksa null. Buyuk/kucuk harf (Turkce kurallariyla) ve
    /// bosluk farki gozetilmez; ifade kelime icinde de yakalanir ('ucuz' -> 'ucuzluk').
    /// </summary>
    public static string? FindBanned(string? text, BrandProfile? brand)
    {
        if (brand is not { BannedPhrases.Count: > 0 } || string.IsNullOrWhiteSpace(text)) return null;

        var haystack = Collapse(text);
        return brand.BannedPhrases.FirstOrDefault(phrase =>
            Collapse(phrase) is { Length: > 0 } needle
            && Turkish.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0);
    }

    /// <summary>
    /// Hashtag yasakli ifadeyi bosluksuz tasir: 'en ucuz' -> '#EnUcuz'. Etiket ve ifade
    /// bosluklari atilarak karsilastirilir.
    /// </summary>
    public static bool IsBannedTag(string tag, BrandProfile? brand)
    {
        if (brand is not { BannedPhrases.Count: > 0 }) return false;

        var bare = tag.TrimStart('#');
        return bare.Length > 0 && brand.BannedPhrases.Any(phrase =>
            phrase.Replace(" ", string.Empty) is { Length: > 0 } needle
            && Turkish.IndexOf(bare, needle, CompareOptions.IgnoreCase) >= 0);
    }

    /// <summary>
    /// Varyanti marka kurallarina uydurur. Emoji istenmiyorsa metinlerden silinir; govde, eylem
    /// cagrisi ya da aciklama yasakli ifade iceriyorsa (ya da yalniz emojiden olusuyorsa) false
    /// doner ve varyant kullanilmamalidir. Yasakli hashtag'ler atilir.
    /// </summary>
    public static bool Apply(ContentVariant variant, BrandProfile? brand)
    {
        if (brand is null) return true;

        if (brand.EmojiUsage == EmojiUsage.None)
        {
            variant.Body = StripEmoji(variant.Body);
            variant.Cta = NullIfEmpty(StripEmoji(variant.Cta));
            variant.Description = NullIfEmpty(StripEmoji(variant.Description));
            variant.CharCount = variant.Body.Length;
        }

        if (variant.Body.Length == 0) return false;

        if (FindBanned(variant.Body, brand) is not null
            || FindBanned(variant.Cta, brand) is not null
            || FindBanned(variant.Description, brand) is not null)
        {
            return false;
        }

        variant.Hashtags = [.. variant.Hashtags.Where(tag => !IsBannedTag(tag, brand))];
        return true;
    }

    /// <summary>
    /// Emoji ve piktogramlari siler; kalan cift bosluklar tekillenir, satir sonlari korunur.
    /// Duz noktalama ve harfler (Turkce dahil) dokunulmaz.
    /// </summary>
    public static string StripEmoji(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            if (!IsEmoji(rune)) sb.Append(rune.ToString());
        }

        var lines = sb.ToString().Split('\n')
            .Select(line => string.Join(' ', line.Split(' ', StringSplitOptions.RemoveEmptyEntries)));
        return string.Join('\n', lines).Trim();
    }

    private static bool IsEmoji(Rune rune) => rune.Value is
        (>= 0x1F000 and <= 0x1FAFF)   // emoji, piktogram, bayrak harfleri
        or (>= 0x2600 and <= 0x27BF)  // cesitli semboller, dingbat
        or (>= 0x2B00 and <= 0x2BFF)  // yildiz, ok (⭐)
        or 0xFE0F or 0x200D or 0x20E3; // gorunum secici, birlestirici, tus kapagi

    private static string Collapse(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
