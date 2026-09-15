using Microsoft.Extensions.Logging;
using SeoCopilot.Application.Abstractions;
using SkiaSharp;

namespace SeoCopilot.Infrastructure.Media;

/// <summary>
/// Ucretsiz gorsel kaynaklarinin cizimi: sitenin fotografini platform oranina kirpar,
/// fotograf yoksa marka karti cizer.
/// </summary>
public sealed class SkiaImageCanvas(ILogger<SkiaImageCanvas> logger) : IImageCanvas
{
    /// <summary>Kaynagin kisa kenari bundan kucukse buyutme bulaniklasir — gorsel kullanilmaz.</summary>
    public const int MinShortSide = 400;

    /// <summary>Genislik/yukseklik bu araligin disindaysa logo ya da serittir, fotograf degil.</summary>
    public const double MaxSourceRatio = 3.2;

    private const int Quality = 90;

    /// <summary>Seffaf PNG (ör. dekupe urun gorseli) JPEG'e siyah zeminle gecmesin.</summary>
    private static readonly SKColor Backdrop = new(244, 245, 247);

    public GeneratedImage? Fit(byte[] source, string aspectRatio)
    {
        try
        {
            using var bitmap = SKBitmap.Decode(source);
            if (bitmap is null) return null;

            var shortSide = Math.Min(bitmap.Width, bitmap.Height);
            var ratio = (double)bitmap.Width / bitmap.Height;
            if (shortSide < MinShortSide || ratio > MaxSourceRatio || ratio < 1 / MaxSourceRatio)
            {
                logger.LogDebug("Site görseli uygun değil: {Width}x{Height}", bitmap.Width, bitmap.Height);
                return null;
            }

            var (width, height) = TargetSize(aspectRatio);
            var crop = CenterCrop(bitmap.Width, bitmap.Height, (double)width / height);

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            surface.Canvas.Clear(Backdrop);

            using var image = SKImage.FromBitmap(bitmap);
            using var paint = new SKPaint { IsAntialias = true };
            surface.Canvas.DrawImage(
                image, crop, SKRect.Create(0, 0, width, height),
                new SKSamplingOptions(SKCubicResampler.Mitchell), paint);

            return Encode(surface, width, height);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Site görseli işlenemedi");
            return null;
        }
    }

    public GeneratedImage Card(string aspectRatio, string seed)
    {
        var (width, height) = TargetSize(aspectRatio);
        var palette = DesignPalette.FromSeed(seed);
        var (from, to) = (palette.Deep, palette.Dark);

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        using (var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(width, height), [from, to], [0f, 1f], SKShaderTileMode.Clamp))
        using (var paint = new SKPaint { Shader = shader })
        {
            canvas.DrawRect(SKRect.Create(0, 0, width, height), paint);
        }

        // Duz zemin cansiz durmasin: sag ustte yumusak bir isik.
        using (var glow = SKShader.CreateRadialGradient(
            new SKPoint(width * 0.82f, height * 0.18f), Math.Max(width, height) * 0.55f,
            [SKColors.White.WithAlpha(46), SKColors.White.WithAlpha(0)], [0f, 1f], SKShaderTileMode.Clamp))
        using (var paint = new SKPaint { Shader = glow })
        {
            canvas.DrawRect(SKRect.Create(0, 0, width, height), paint);
        }

        return Encode(surface, width, height);
    }

    /// <summary>Platformlarin onerdigi paylasim olculeri.</summary>
    private static (int Width, int Height) TargetSize(string aspectRatio) => aspectRatio switch
    {
        "16:9" => (1200, 675),
        "4:5" => (1080, 1350),
        "9:16" => (1080, 1920),
        _ => (1080, 1080)
    };

    /// <summary>Kaynagin ortasindan hedef oranda en buyuk dikdortgen.</summary>
    private static SKRect CenterCrop(int width, int height, double targetRatio)
    {
        var sourceRatio = (double)width / height;

        if (sourceRatio > targetRatio)
        {
            var cropWidth = (float)(height * targetRatio);
            return SKRect.Create((width - cropWidth) / 2, 0, cropWidth, height);
        }

        var cropHeight = (float)(width / targetRatio);
        return SKRect.Create(0, (height - cropHeight) / 2, width, cropHeight);
    }

    private static GeneratedImage Encode(SKSurface surface, int width, int height)
    {
        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, Quality);
        return new GeneratedImage(data.ToArray(), "image/jpeg", width, height, string.Empty);
    }
}
