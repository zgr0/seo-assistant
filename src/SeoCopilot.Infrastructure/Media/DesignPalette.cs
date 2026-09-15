using System.Security.Cryptography;
using System.Text;
using SkiaSharp;

namespace SeoCopilot.Infrastructure.Media;

/// <param name="Deep">Panel ve zemin rengi — beyaz metin ustunde okunur kalacak kadar koyu.</param>
/// <param name="Dark">Zemin gecisinin ikinci, daha koyu rengi.</param>
/// <param name="Accent">Koyu zeminde vurgu (cubuk, nokta, alinti isareti).</param>
/// <param name="AccentOnLight">Beyaz kart ustunde okunur vurgu.</param>
internal readonly record struct DesignPalette(SKColor Deep, SKColor Dark, SKColor Accent, SKColor AccentOnLight)
{
    /// <summary>Koyu metin rengi (beyaz kart ustu).</summary>
    public static readonly SKColor Ink = new(17, 18, 24);

    /// <summary>Tohumdan turetilen ton; marka kartiyla ayni formul — kart ve tasarim uyumlu kalir.</summary>
    public static float HueOf(string seed)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed.ToLowerInvariant()));
        return hash[0] * 360f / 256f;
    }

    /// <summary>Marka kartinin gecis renkleri (bkz. <see cref="SkiaImageCanvas"/>).</summary>
    public static DesignPalette FromSeed(string seed)
    {
        var hue = HueOf(seed);
        return new DesignPalette(
            SKColor.FromHsl(hue, 62, 30),
            SKColor.FromHsl((hue + 38) % 360, 58, 18),
            SKColor.FromHsl(hue, 85, 68),
            SKColor.FromHsl(hue, 70, 40));
    }

    /// <summary>
    /// Gorselin baskin rengine uyan palet: panel fotografla ayni ailede durur. Gorsel renksizse
    /// (gri, siyah-beyaz) tohumdan, tohum da yoksa notr laciverte duser.
    /// </summary>
    public static DesignPalette ForImage(SKBitmap bitmap, string? seed)
    {
        if (DominantHue(bitmap) is { } hue)
        {
            return new DesignPalette(
                SKColor.FromHsl(hue, 48, 22),
                SKColor.FromHsl((hue + 18) % 360, 45, 13),
                SKColor.FromHsl(hue, 82, 66),
                SKColor.FromHsl(hue, 68, 38));
        }

        return FromSeed(seed is { Length: > 0 } ? seed : "seocopilot");
    }

    /// <summary>Doygunluk agirlikli ton histogrami; anlamli renk yoksa null.</summary>
    private static float? DominantHue(SKBitmap bitmap)
    {
        const int Sample = 32;
        const int Bins = 12;

        using var small = bitmap.Resize(new SKImageInfo(Sample, Sample), new SKSamplingOptions(SKFilterMode.Linear));
        if (small is null) return null;

        var weights = new double[Bins];
        var hueSums = new double[Bins];

        for (var y = 0; y < Sample; y++)
        {
            for (var x = 0; x < Sample; x++)
            {
                small.GetPixel(x, y).ToHsl(out var h, out var s, out var l);

                // Soluk ve cok acik/koyu pikseller rengi temsil etmez.
                if (s < 22 || l < 12 || l > 88) continue;

                var weight = s * (1 - (Math.Abs(l - 50) / 50.0));
                var bin = (int)(h / 360f * Bins) % Bins;
                weights[bin] += weight;
                hueSums[bin] += h * weight;
            }
        }

        var best = Array.IndexOf(weights, weights.Max());

        // Toplam agirlik gorselin ~%8'inin orta doygunlukta renk tasimasina esit degilse renksiz say.
        return weights.Sum() < Sample * Sample * 0.08 * 25 ? null : (float)(hueSums[best] / weights[best]);
    }
}
