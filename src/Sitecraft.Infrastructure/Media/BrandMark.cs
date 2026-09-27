using SkiaSharp;

namespace Sitecraft.Infrastructure.Media;

/// <summary>
/// Gorseldeki marka satiri: logo varsa beyaz yuvarlak rozet icinde logo, yoksa vurgu noktasi;
/// ardindan marka adi. Rozet koyu panelde, fotografta ve beyaz kartta ayni sekilde okunur —
/// logonun kendi renkleri zeminden bagimsiz kalir.
/// </summary>
internal static class BrandMark
{
    /// <summary>Rozet yuksekligi — marka puntosunun kati.</summary>
    private const float BadgeHeightRatio = 1.5f;

    /// <summary>Genis yazi logolari satiri kaplamasin: rozet en fazla bu kadar punto genisliginde.</summary>
    private const float MaxBadgeWidthRatio = 5.5f;

    /// <summary>Rozet ic boslugu — rozet yuksekliginin orani.</summary>
    private const float BadgePaddingRatio = 0.14f;

    /// <param name="baseline">Metnin taban cizgisi; rozet ve nokta metnin ortasina hizalanir.</param>
    public static void Draw(
        SKCanvas canvas, SKImage? logo, string text, float x, float baseline,
        SKFont font, SKColor textColor, SKColor dot)
    {
        // Kucuk harf yuksekliginin ortasi — nokta ve rozet metinle ayni hizada dursun.
        var middle = baseline - (font.Size * 0.36f);
        using var paint = new SKPaint { IsAntialias = true };

        float textX;
        if (logo is not null)
        {
            textX = x + DrawBadge(canvas, logo, x, middle, font.Size) + (font.Size * 0.5f);
        }
        else
        {
            var radius = font.Size * 0.24f;
            paint.Color = dot;
            canvas.DrawCircle(x + radius, middle, radius, paint);
            textX = x + (radius * 2) + (font.Size * 0.45f);
        }

        paint.Color = textColor;
        canvas.DrawText(text, textX, baseline, SKTextAlign.Left, font, paint);
    }

    /// <summary>Logo cozulemezse null — cagiran taraf noktaya duser.</summary>
    public static SKImage? Decode(byte[]? logo) =>
        logo is { Length: > 0 } ? SKImage.FromEncodedData(logo) : null;

    /// <summary>Rozeti cizer, genisligini doner.</summary>
    private static float DrawBadge(SKCanvas canvas, SKImage logo, float x, float middle, float fontSize)
    {
        var height = fontSize * BadgeHeightRatio;
        var padding = height * BadgePaddingRatio;
        var inner = height - (2 * padding);

        // Oran korunur; cok genis logo sigdirilir (yuksekligi azalir, ortalanir).
        var scale = Math.Min(inner / logo.Height, ((fontSize * MaxBadgeWidthRatio) - (2 * padding)) / logo.Width);
        var logoWidth = logo.Width * scale;
        var logoHeight = logo.Height * scale;
        var width = Math.Max(height, logoWidth + (2 * padding));

        var badge = SKRect.Create(x, middle - (height / 2), width, height);
        var radius = height * 0.28f;

        using (var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White.WithAlpha(245),
            ImageFilter = SKImageFilter.CreateDropShadow(
                0, height * 0.04f, height * 0.08f, height * 0.08f, SKColors.Black.WithAlpha(60))
        })
        {
            canvas.DrawRoundRect(badge, radius, radius, paint);
        }

        var target = SKRect.Create(
            badge.MidX - (logoWidth / 2), badge.MidY - (logoHeight / 2), logoWidth, logoHeight);
        using (var paint = new SKPaint { IsAntialias = true })
        {
            canvas.DrawImage(logo, target, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);
        }

        return width;
    }
}
