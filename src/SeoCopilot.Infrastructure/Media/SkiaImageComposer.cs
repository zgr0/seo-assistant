using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;
using SkiaSharp;

namespace SeoCopilot.Infrastructure.Media;

public sealed class ImageOverlayOptions
{
    public const string Section = "ImageOverlay";

    /// <summary>Kapatilirsa gorsele yazi basilmaz, yalniz ham gorsel saklanir.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>JPEG kalitesi (1-100).</summary>
    public int Quality { get; set; } = 90;

    /// <summary>Kenar boslugu — genisligin orani.</summary>
    public double MarginRatio { get; set; } = 0.06;

    /// <summary>Baslik baslangic punto orani; sigmazsa kucultulur.</summary>
    public double HeadlineSizeRatio { get; set; } = 0.078;

    public double BrandSizeRatio { get; set; } = 0.032;

    /// <summary>Baslik en fazla bu kadar satira sarar; tasan kisim kirpilir.</summary>
    public int MaxHeadlineLines { get; set; } = 3;
}

/// <summary>
/// Gorselin uzerine baslik ve marka satirini basar. Okunurlugu garantilemek icin metnin
/// arkasina gecisli bir perde cizilir; perdenin rengi gorselin alt bolgesinin parlakligina
/// gore secilir.
/// </summary>
public sealed class SkiaImageComposer(
    IOptions<ImageOverlayOptions> options, ILogger<SkiaImageComposer> logger)
    : ISocialImageComposer
{
    private readonly ImageOverlayOptions _opt = options.Value;

    /// <summary>Bu parlakligin ustundeki gorsellerde acik perde + koyu metin kullanilir.</summary>
    private const double BrightThreshold = 0.72;

    /// <summary>Perdenin basladigi yukseklik orani — ust kisim dokunulmadan kalir.</summary>
    private const float ScrimStart = 0.42f;

    public ComposedImage? Compose(byte[] image, ImageCaption caption)
    {
        if (!_opt.Enabled) return null;

        try
        {
            using var bitmap = SKBitmap.Decode(image);
            if (bitmap is null)
            {
                logger.LogWarning("Görsel çözümlenemedi — yazı basılamadı");
                return null;
            }

            using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
            var canvas = surface.Canvas;
            canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default);

            var bright = IsBright(bitmap);
            DrawScrim(canvas, bitmap.Width, bitmap.Height, bright);
            DrawCaption(canvas, bitmap.Width, bitmap.Height, caption, bright);

            using var snapshot = surface.Snapshot();
            using var encoded = snapshot.Encode(SKEncodedImageFormat.Jpeg, _opt.Quality);

            return new ComposedImage(
                encoded.ToArray(), "image/jpeg", bitmap.Width, bitmap.Height);
        }
        catch (Exception ex)
        {
            // Yazi kozmetiktir — cagiran taraf ham gorselle devam eder.
            logger.LogWarning(ex, "Görsele yazı basılamadı");
            return null;
        }
    }

    /// <summary>Metnin oturacagi alt bolgenin ortalama parlakligi (0-1).</summary>
    private static bool IsBright(SKBitmap bitmap)
    {
        var top = (int)(bitmap.Height * ScrimStart);
        var stepX = Math.Max(1, bitmap.Width / 32);
        var stepY = Math.Max(1, (bitmap.Height - top) / 32);

        double total = 0;
        var samples = 0;

        for (var y = top; y < bitmap.Height; y += stepY)
        {
            for (var x = 0; x < bitmap.Width; x += stepX)
            {
                var pixel = bitmap.GetPixel(x, y);
                // Rec. 601 luma — goz duyarliligina gore agirliklandirilmis parlaklik.
                total += (0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue) / 255.0;
                samples++;
            }
        }

        return samples > 0 && total / samples > BrightThreshold;
    }

    private static void DrawScrim(SKCanvas canvas, int width, int height, bool bright)
    {
        var start = height * ScrimStart;
        var scrim = bright ? SKColors.White : SKColors.Black;

        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, start),
            new SKPoint(0, height),
            [scrim.WithAlpha(0), scrim.WithAlpha(bright ? (byte)210 : (byte)190)],
            [0f, 1f],
            SKShaderTileMode.Clamp);

        using var paint = new SKPaint { Shader = shader };
        canvas.DrawRect(SKRect.Create(0, start, width, height - start), paint);
    }

    private void DrawCaption(
        SKCanvas canvas, int width, int height, ImageCaption caption, bool bright)
    {
        var margin = (float)(width * _opt.MarginRatio);
        var maxWidth = width - (2 * margin);

        var headlineColor = bright ? new SKColor(17, 18, 24) : SKColors.White;
        var brandColor = headlineColor.WithAlpha(200);

        using var headlineFont = new SKFont(EmbeddedFonts.Sans)
        {
            Size = (float)(width * _opt.HeadlineSizeRatio),
            Embolden = true,
            Subpixel = true
        };

        // Satir sayisi sinirina sigana kadar puntoyu kucult.
        List<string> lines;
        while (true)
        {
            lines = Wrap(caption.Headline, headlineFont, maxWidth);
            if (lines.Count <= _opt.MaxHeadlineLines || headlineFont.Size <= width * 0.04f) break;
            headlineFont.Size *= 0.92f;
        }

        if (lines.Count > _opt.MaxHeadlineLines)
            lines = [.. lines.Take(_opt.MaxHeadlineLines)];

        using var brandFont = new SKFont(EmbeddedFonts.Sans)
        {
            Size = (float)(width * _opt.BrandSizeRatio),
            Subpixel = true
        };

        var headlineLeading = headlineFont.Size * 1.18f;
        var brandLeading = string.IsNullOrWhiteSpace(caption.BrandLine) ? 0 : brandFont.Size * 1.6f;

        // Blok alttan yukari yerlesir: once marka satiri, ustunde baslik satirlari.
        var baseline = height - margin;

        using var paint = new SKPaint { IsAntialias = true };

        if (caption.BrandLine is { Length: > 0 } brand)
        {
            paint.Color = brandColor;
            canvas.DrawText(brand, margin, baseline, SKTextAlign.Left, brandFont, paint);
            baseline -= brandLeading;
        }

        paint.Color = headlineColor;
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            canvas.DrawText(lines[i], margin, baseline, SKTextAlign.Left, headlineFont, paint);
            baseline -= headlineLeading;
        }
    }

    /// <summary>Kelime bazli sarma; tek basina sigmayan kelime oldugu gibi birakilir.</summary>
    private static List<string> Wrap(string text, SKFont font, float maxWidth)
    {
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (font.MeasureText(candidate) <= maxWidth || current.Length == 0)
            {
                current = candidate;
                continue;
            }

            lines.Add(current);
            current = word;
        }

        if (current.Length > 0) lines.Add(current);
        return lines;
    }
}
