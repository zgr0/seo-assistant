using SkiaSharp;

namespace SeoCopilot.Infrastructure.Media;

/// <summary>Kutuya yerlestirilmis cok satirli metin: secilen punto ve satirlar.</summary>
internal sealed class TextBlock(SKFont font, List<string> lines, float leading) : IDisposable
{
    public SKFont Font { get; } = font;
    public IReadOnlyList<string> Lines { get; } = lines;
    public float LineHeight => Font.Size * leading;

    /// <summary>Ilk satirin ust hizasindan son satirin alt cizgisinin biraz altina kadar.</summary>
    public float Height => Lines.Count == 0
        ? 0
        : Ascent + ((Lines.Count - 1) * LineHeight) + (Font.Size * DescentRatio);

    /// <summary>Ust hizadan ilk satirin taban cizgisine mesafe.</summary>
    public float Ascent => Font.Size * AscentRatio;

    // Inter icin olculmus yaklasik oranlar; metrik tablosu degisken fontta tutarsiz donebiliyor.
    private const float AscentRatio = 0.93f;
    private const float DescentRatio = 0.24f;

    /// <summary>Blogu ust hizasi <paramref name="top"/> olacak sekilde cizer; alt kenari doner.</summary>
    public float Draw(SKCanvas canvas, float x, float top, SKColor color, SKTextAlign align = SKTextAlign.Left)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = color };

        var baseline = top + Ascent;
        foreach (var line in Lines)
        {
            canvas.DrawText(line, x, baseline, align, Font, paint);
            baseline += LineHeight;
        }

        return top + Height;
    }

    public void Dispose() => Font.Dispose();
}

internal static class SkiaText
{
    /// <summary>
    /// Metni genislige sarar; satir siniri, yukseklik siniri ya da tek basina sigmayan uzun kelime
    /// varsa puntoyu adim adim kucultur. En kucuk puntoda da sigmazsa son satir uc noktayla biter.
    /// </summary>
    public static TextBlock Fit(
        string text, float size, float minSize, float maxWidth, int maxLines,
        float maxHeight = float.MaxValue, bool bold = false, float leading = 1.16f)
    {
        var font = new SKFont(EmbeddedFonts.Sans) { Size = size, Embolden = bold, Subpixel = true };
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        List<string> lines;
        while (true)
        {
            lines = Wrap(words, font, maxWidth);
            var height = (font.Size * 1.17f) + ((Math.Min(lines.Count, maxLines) - 1) * font.Size * leading);
            var widestWord = words.Length == 0 ? 0 : words.Max(w => font.MeasureText(w));

            var fits = lines.Count <= maxLines && height <= maxHeight && widestWord <= maxWidth;
            if (fits || font.Size <= minSize) break;

            font.Size = Math.Max(minSize, font.Size * 0.93f);
        }

        if (lines.Count > maxLines)
        {
            lines = [.. lines.Take(maxLines)];
            lines[^1] = Ellipsize(lines[^1], font, maxWidth);
        }

        // Yukseklik en kucuk puntoda da tasiyorsa sigan satir kadar kalir.
        while (lines.Count > 1
            && (font.Size * 1.17f) + ((lines.Count - 1) * font.Size * leading) > maxHeight)
        {
            lines.RemoveAt(lines.Count - 1);
            lines[^1] = Ellipsize(lines[^1], font, maxWidth);
        }

        return new TextBlock(font, lines, leading);
    }

    /// <summary>Kelime bazli sarma; tek basina sigmayan kelime kendi satirinda kalir.</summary>
    public static List<string> Wrap(IEnumerable<string> words, SKFont font, float maxWidth)
    {
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in words)
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

    /// <summary>Satiri kelime kelime kisaltip sonuna uc nokta koyar.</summary>
    private static string Ellipsize(string line, SKFont font, float maxWidth)
    {
        var text = line.TrimEnd('…', '.', ',', ';', ':', ' ');
        while (text.Length > 0 && font.MeasureText(text + "…") > maxWidth)
        {
            var cut = text.LastIndexOf(' ');
            text = cut > 0 ? text[..cut] : text[..^1];
            text = text.TrimEnd('.', ',', ';', ':', ' ');
        }

        return text + "…";
    }
}
