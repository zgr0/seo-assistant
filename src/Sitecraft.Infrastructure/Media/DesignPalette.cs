using System.Security.Cryptography;
using System.Text;
using Sitecraft.Application.Abstractions;
using SkiaSharp;

namespace Sitecraft.Infrastructure.Media;

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

        return FromSeed(seed is { Length: > 0 } ? seed : "sitecraft");
    }

    /// <summary>Panel/zemin rengi en fazla bu aciklikta — ustundeki beyaz metin okunur kalsin.</summary>
    private const float MaxDeepLightness = 32;

    /// <summary>
    /// HSL acikligi algiya denk degil: ayni aciklikta sari kirmizidan cok daha parlak gorunur.
    /// Zemin bu algisal parlakligin (Rec. 601 luma) altina inene kadar koyulastirilir.
    /// </summary>
    private const double MaxDeepLuma = 0.28;

    /// <summary>Koyu zemindeki vurgu en az bu aciklikta, beyaz karttaki en fazla bu kadar.</summary>
    private const float MinAccentOnDarkLightness = 55;
    private const float MaxAccentOnLightLightness = 42;

    /// <summary>
    /// Markanin gorsel kimligi: ana renk verilmisse palet ondan kurulur ve fotografin/tohumun
    /// onune gecer; yalniz vurgu verilmisse <paramref name="fallback"/> paletinin vurgusu degisir.
    /// Gecersiz renk yok sayilir.
    /// </summary>
    public static DesignPalette For(BrandStyle? brand, Func<DesignPalette> fallback)
    {
        var primary = Parse(brand?.PrimaryColor);
        var accent = Parse(brand?.AccentColor);

        if (primary is { } main) return FromBrand(main, accent);
        return accent is { } only ? fallback().WithAccent(only) : fallback();
    }

    /// <summary>
    /// Ton ve doygunluk markadan gelir; aciklik okunurluk icin sinirlanir. Acik bir marka rengi
    /// (sari, pastel) beyaz metnin arkasinda kaybolmasin diye ayni tonda koyulastirilir, zaten
    /// koyu renk oldugu gibi kalir.
    /// </summary>
    public static DesignPalette FromBrand(SKColor primary, SKColor? accent = null)
    {
        primary.ToHsl(out var hue, out var saturation, out var lightness);

        var deep = Math.Min(lightness, MaxDeepLightness);
        while (deep > 8 && Luma(SKColor.FromHsl(hue, saturation, deep)) > MaxDeepLuma) deep -= 2;

        var palette = new DesignPalette(
            SKColor.FromHsl(hue, saturation, deep),
            SKColor.FromHsl((hue + 18) % 360, saturation, deep * 0.6f),
            SKColor.FromHsl(hue, Math.Max(saturation, 60), 66),
            SKColor.FromHsl(hue, Math.Max(saturation, 55), 38));

        return accent is { } given ? palette.WithAccent(given) : palette;
    }

    /// <summary>Vurgu markadan; koyu zeminde ve beyaz kartta okunacak acikliga cekilir.</summary>
    public DesignPalette WithAccent(SKColor accent)
    {
        accent.ToHsl(out var hue, out var saturation, out var lightness);
        return this with
        {
            Accent = SKColor.FromHsl(hue, saturation, Math.Max(lightness, MinAccentOnDarkLightness)),
            AccentOnLight = SKColor.FromHsl(hue, saturation, Math.Min(lightness, MaxAccentOnLightLightness))
        };
    }

    private static double Luma(SKColor c) => ((0.299 * c.Red) + (0.587 * c.Green) + (0.114 * c.Blue)) / 255.0;

    private static SKColor? Parse(string? hex) =>
        hex is { Length: > 0 } && SKColor.TryParse(hex, out var color) ? color : null;

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
