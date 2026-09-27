using System.Text;
using System.Text.RegularExpressions;

namespace Sitecraft.Application.Services.Content;

/// <summary>
/// Marka profili alanlarinin sinirlari ve normalizasyonu. Profil istemlere ve gorsellere
/// dogrudan girdigi icin kayit aninda temizlenir: gecersiz deger 400 doner, bicim farklari
/// (hashtag'in '#'i, rengin buyuk/kucuk harfi, hesap adinin '@'i) sessizce duzeltilir.
/// </summary>
public static partial class BrandProfileRules
{
    public const int MaxNameChars = 200;
    public const int MaxTargetAudienceChars = 1024;

    /// <summary>Ek baglam sistem istemine aynen girer — istem boyutu icin sinirli.</summary>
    public const int MaxExtraContextChars = 2000;

    public const int MaxBannedPhrases = 50;
    public const int MaxBannedPhraseChars = 100;

    public const int MaxHashtags = 10;
    public const int MaxHashtagChars = 50;

    public const int MaxHandleChars = 100;

    /// <summary>Zorunlu ad: bos olamaz, sinir asilamaz.</summary>
    public static string Name(string? value)
    {
        var name = value?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Profil adı zorunlu");
        if (name.Length > MaxNameChars)
            throw new InvalidOperationException($"Profil adı en fazla {MaxNameChars} karakter olabilir");
        return name;
    }

    /// <summary>Serbest metin: bos ise null (alan temizlenir), sinir asilirsa 400.</summary>
    public static string? Text(string? value, int maxChars, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var text = value.Trim();
        if (text.Length > maxChars)
            throw new InvalidOperationException($"{label} en fazla {maxChars} karakter olabilir");
        return text;
    }

    /// <summary>Kirpilir, bosluklar tekillenir, tekrarlar (buyuk/kucuk harf farksiz) atilir.</summary>
    public static List<string> BannedPhrases(IEnumerable<string>? values)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values ?? [])
        {
            var phrase = Collapse(value);
            if (phrase.Length == 0 || !seen.Add(phrase)) continue;

            if (phrase.Length > MaxBannedPhraseChars)
                throw new InvalidOperationException(
                    $"Yasaklı ifade en fazla {MaxBannedPhraseChars} karakter olabilir: '{phrase[..20]}…'");
            result.Add(phrase);
        }

        if (result.Count > MaxBannedPhrases)
            throw new InvalidOperationException($"En fazla {MaxBannedPhrases} yasaklı ifade girilebilir");
        return result;
    }

    /// <summary>
    /// Kullanicinin yazdigi hashtag korunur (buyuk harf, Turkce karakter — platformlar destekler);
    /// yalniz '#' eklenir, bosluk ve noktalama atilir. Tekrarlar buyuk/kucuk harf farksiz elenir.
    /// </summary>
    public static List<string> Hashtags(IEnumerable<string>? values)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values ?? [])
        {
            var sb = new StringBuilder();
            foreach (var ch in value ?? string.Empty)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            }

            if (sb.Length == 0) continue;
            if (sb.Length > MaxHashtagChars)
                throw new InvalidOperationException($"Hashtag en fazla {MaxHashtagChars} karakter olabilir: '#{sb.ToString()[..20]}…'");

            var tag = $"#{sb}";
            if (seen.Add(tag)) result.Add(tag);
        }

        if (result.Count > MaxHashtags)
            throw new InvalidOperationException($"En fazla {MaxHashtags} varsayılan hashtag girilebilir");
        return result;
    }

    /// <summary>'#1a2b3c' ya da '1A2B3C' kabul edilir, '#1A2B3C' doner. Bos ise null (renk temizlenir).</summary>
    public static string? Color(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var hex = value.Trim().TrimStart('#');
        if (!HexColor().IsMatch(hex))
            throw new InvalidOperationException($"Geçersiz {label}: '{value}' — #RRGGBB bekleniyor");
        return $"#{hex.ToUpperInvariant()}";
    }

    /// <summary>
    /// Platform kodu -> hesap adi. Kod bilinen platformlardan olmali; deger '@ornek', 'ornek' ya da
    /// profil adresi olabilir, '@' ve adres kismi atilir. Bos deger o platformu kaldirir.
    /// </summary>
    public static Dictionary<string, string> Handles(
        IReadOnlyDictionary<string, string?>? values, IReadOnlySet<string> platformCodes)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (rawCode, rawHandle) in values ?? new Dictionary<string, string?>())
        {
            var code = rawCode.Trim().ToLowerInvariant();
            if (!platformCodes.Contains(code))
                throw new InvalidOperationException($"Bilinmeyen platform: '{rawCode}'");

            var handle = Handle(rawHandle);
            if (handle is null) continue;

            if (handle.Length > MaxHandleChars || !HandleChars().IsMatch(handle))
                throw new InvalidOperationException($"Geçersiz hesap adı ({code}): '{rawHandle}'");
            result[code] = handle;
        }

        return result;
    }

    /// <summary>'https://instagram.com/ornek/' -> 'ornek'; '@ornek' -> 'ornek'.</summary>
    private static string? Handle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var handle = value.Trim();
        if (Uri.TryCreate(handle, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            handle = uri.AbsolutePath.Trim('/').Split('/').LastOrDefault() ?? string.Empty;

        handle = handle.TrimStart('@').Trim();
        return handle.Length == 0 ? null : handle;
    }

    private static string Collapse(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [GeneratedRegex("^[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();

    /// <summary>Platform kullanici adlari: harf, rakam, nokta, alt cizgi, tire.</summary>
    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex HandleChars();
}
