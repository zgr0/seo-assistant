using System.Reflection;
using SkiaSharp;

namespace SeoCopilot.Infrastructure.Media;

/// <summary>
/// Yazi tipi derlemeye gomulur. Konteynerin sistem fontlarina guvenilemez: Turkce glifler
/// (İ, ı, ğ, ş) cogu temel imajda eksiktir ve yerine bos kutu cizilir.
/// </summary>
internal static class EmbeddedFonts
{
    private const string ResourceName = "SeoCopilot.Infrastructure.Media.Fonts.Inter.ttf";

    private static readonly Lazy<SKTypeface> Instance = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Inter (SIL OFL 1.1) — degisken eksenli; varsayilan ornek Regular agirliktir.</summary>
    public static SKTypeface Sans => Instance.Value;

    private static SKTypeface Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Gömülü yazı tipi bulunamadı: {ResourceName}");

        // SKTypeface akisi tuketir; bellege alip oradan yuklemek daha guvenli.
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return SKTypeface.FromData(SKData.CreateCopy(buffer.ToArray()))
            ?? throw new InvalidOperationException("Gömülü yazı tipi çözümlenemedi");
    }
}
