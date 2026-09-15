using SeoCopilot.Application.Abstractions;
using SkiaSharp;

namespace SeoCopilot.Infrastructure.Media;

/// <summary>
/// Gonderi tasarim sablonlari (<see cref="ImageTemplate"/>). Tum olculer gorselin kisa kenarina
/// oranlidir — 1080x1080 kare ile 1200x675 yatayda ayni tasarim dili korunur. Yatay gorselde
/// metin yana, kare/dikeyde alta yerlesir.
/// </summary>
internal sealed class SkiaPostTemplates
{
    private static readonly SKColor Translucent = SKColors.White.WithAlpha(190);

    private readonly SKCanvas _canvas;
    private readonly SKImage _source;
    private readonly ImageCaption _caption;
    private readonly DesignPalette _palette;
    private readonly int _width;
    private readonly int _height;

    /// <summary>Olcu birimi: kisa kenar.</summary>
    private readonly float _unit;

    private readonly bool _landscape;

    public SkiaPostTemplates(SKCanvas canvas, SKBitmap source, ImageCaption caption)
    {
        _canvas = canvas;
        _source = SKImage.FromBitmap(source);
        _caption = caption;
        _palette = DesignPalette.ForImage(source, caption.ColorSeed);
        _width = source.Width;
        _height = source.Height;
        _unit = Math.Min(_width, _height);
        _landscape = _width > _height * 1.15f;
    }

    public void Draw(ImageTemplate template)
    {
        try
        {
            switch (template)
            {
                case ImageTemplate.Split: Split(); break;
                case ImageTemplate.Framed: Framed(); break;
                case ImageTemplate.Label: Label(); break;
                case ImageTemplate.Quote when _caption.Subline is { Length: > 0 }: Quote(); break;
                default: Poster(); break;
            }
        }
        finally
        {
            _source.Dispose();
        }
    }

    // --- sablonlar ---

    /// <summary>Fotograf bir yarida, digerinde fotografin renginden panel.</summary>
    private void Split()
    {
        var photo = _landscape
            ? SKRect.Create(0, 0, _width * 0.54f, _height)
            : SKRect.Create(0, 0, _width, _height * 0.56f);
        var panel = _landscape
            ? new SKRect(photo.Right, 0, _width, _height)
            : new SKRect(0, photo.Bottom, _width, _height);

        DrawCover(photo);
        Fill(panel, _palette.Deep);

        // Ek yerinde ince vurgu seridi — iki alan birbirine yapismis gibi durmasin.
        var seam = _unit * 0.012f;
        Fill(_landscape
            ? SKRect.Create(panel.Left, 0, seam, _height)
            : SKRect.Create(0, panel.Top, _width, seam), _palette.Accent);

        var pad = _unit * 0.07f;
        DrawTextColumn(
            new SKRect(panel.Left + pad, panel.Top + pad + seam, panel.Right - pad, panel.Bottom - pad),
            headlineSize: _unit * (_landscape ? 0.068f : 0.064f),
            maxHeadlineLines: _landscape ? 4 : 3,
            centerVertically: _landscape);
    }

    /// <summary>Koyu gecisli zeminde yuvarlak koseli, golgeli fotograf.</summary>
    private void Framed()
    {
        DrawGradientBackground();

        var margin = _unit * 0.07f;
        var photo = _landscape
            ? new SKRect(margin, margin, _width * 0.52f, _height - margin)
            : new SKRect(margin, margin, _width - margin, margin + ((_height - (2 * margin)) * 0.6f));
        var radius = _unit * 0.035f;

        using (var shadow = new SKPaint
        {
            IsAntialias = true,
            Color = _palette.Dark,
            ImageFilter = SKImageFilter.CreateDropShadow(
                0, _unit * 0.014f, _unit * 0.025f, _unit * 0.025f, SKColors.Black.WithAlpha(150))
        })
        {
            _canvas.DrawRoundRect(photo, radius, radius, shadow);
        }

        _canvas.Save();
        _canvas.ClipRoundRect(new SKRoundRect(photo, radius), SKClipOperation.Intersect, antialias: true);
        DrawCover(photo);
        _canvas.Restore();

        var text = _landscape
            ? new SKRect(photo.Right + (margin * 0.9f), margin, _width - margin, _height - margin)
            : new SKRect(margin, photo.Bottom + (margin * 0.8f), _width - margin, _height - margin);

        DrawTextColumn(
            text,
            headlineSize: _unit * (_landscape ? 0.066f : 0.062f),
            maxHeadlineLines: _landscape ? 4 : 2,
            centerVertically: _landscape);
    }

    /// <summary>Tam ekran fotograf; alt kosede beyaz etiket kartinda baslik.</summary>
    private void Label()
    {
        DrawCover(SKRect.Create(0, 0, _width, _height));

        // Kartin oturdugu alt bolgeye hafif golge: parlak fotografta kart kaybolmasin.
        using (var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, _height * 0.45f), new SKPoint(0, _height),
            [SKColors.Black.WithAlpha(0), SKColors.Black.WithAlpha(110)], [0f, 1f], SKShaderTileMode.Clamp))
        using (var paint = new SKPaint { Shader = shader })
        {
            _canvas.DrawRect(SKRect.Create(0, _height * 0.45f, _width, _height * 0.55f), paint);
        }

        var margin = _unit * 0.06f;
        var pad = _unit * 0.05f;
        var cardWidth = _landscape ? _width * 0.58f : _width - (2 * margin);
        var innerWidth = cardWidth - (2 * pad);

        var barHeight = _unit * 0.012f;
        var barGap = _unit * 0.028f;

        using var headline = SkiaText.Fit(
            _caption.Headline, _unit * 0.062f, _unit * 0.036f, innerWidth,
            maxLines: 3, maxHeight: _height * 0.4f, bold: true);
        using var brand = BrandFont(_unit * 0.03f);

        var brandBlock = _caption.BrandLine is { Length: > 0 } ? brand.Size * 1.9f : 0;
        var cardHeight = pad + barHeight + barGap + headline.Height + brandBlock + pad;
        var card = SKRect.Create(margin, _height - margin - cardHeight, cardWidth, cardHeight);
        var radius = _unit * 0.028f;

        using (var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White.WithAlpha(246),
            ImageFilter = SKImageFilter.CreateDropShadow(
                0, _unit * 0.01f, _unit * 0.02f, _unit * 0.02f, SKColors.Black.WithAlpha(90))
        })
        {
            _canvas.DrawRoundRect(card, radius, radius, paint);
        }

        var x = card.Left + pad;
        var y = card.Top + pad;
        Bar(x, y, _unit * 0.09f, barHeight, _palette.AccentOnLight);
        y += barHeight + barGap;

        y = headline.Draw(_canvas, x, y, DesignPalette.Ink);

        if (_caption.BrandLine is { Length: > 0 } brandLine)
            DrawBrand(brandLine, x, y + (brand.Size * 1.55f), brand, _palette.AccentOnLight, _palette.AccentOnLight);
    }

    /// <summary>Fotografsiz: desenli marka zemini ve buyuk baslik.</summary>
    private void Poster()
    {
        DrawRaw();
        DrawPattern();

        var margin = _unit * 0.09f;
        var barHeight = _unit * 0.016f;
        Bar(margin, margin, _unit * 0.12f, barHeight, _palette.Accent);

        using var brand = BrandFont(_unit * 0.032f);
        var footer = DrawFooter(margin, brand);

        var area = new SKRect(margin, margin + barHeight + (_unit * 0.05f), _width - margin, footer - (_unit * 0.05f));
        DrawTextColumn(
            area,
            headlineSize: _unit * (_landscape ? 0.085f : 0.1f),
            maxHeadlineLines: 4,
            centerVertically: true,
            withBrand: false,
            withBar: false);
    }

    /// <summary>Fotografsiz: sayfadan bir cumle alinti, baslik kaynak satiri.</summary>
    private void Quote()
    {
        DrawRaw();

        var margin = _unit * 0.09f;

        // Buyuk tirnak isareti — alinti oldugu ilk bakista anlasilsin.
        using (var quoteFont = new SKFont(EmbeddedFonts.Sans) { Size = _unit * 0.42f, Embolden = true })
        using (var paint = new SKPaint { IsAntialias = true, Color = _palette.Accent.WithAlpha(170) })
        {
            _canvas.DrawText("“", margin - (_unit * 0.02f), margin + (_unit * 0.3f), SKTextAlign.Left, quoteFont, paint);
        }

        using var brand = BrandFont(_unit * 0.032f);
        var footer = DrawFooter(margin, brand);

        var top = margin + (_unit * 0.25f);
        var width = _width - (2 * margin);
        var available = footer - (_unit * 0.05f) - top;

        using var source = SkiaText.Fit(
            $"— {_caption.Headline}", _unit * 0.038f, _unit * 0.03f, width, maxLines: 2, bold: true);
        var sourceBlock = source.Height + (_unit * 0.045f);

        using var quote = SkiaText.Fit(
            _caption.Subline!, _unit * (_landscape ? 0.062f : 0.07f), _unit * 0.036f, width,
            maxLines: _landscape ? 4 : 6, maxHeight: available - sourceBlock, bold: true, leading: 1.22f);

        var y = quote.Draw(_canvas, margin, top, SKColors.White);
        source.Draw(_canvas, margin, y + (_unit * 0.045f), _palette.Accent);
    }

    // --- ortak parcalar ---

    /// <summary>
    /// Vurgu cubugu, baslik, alt metin ve marka satirindan olusan metin sutunu. Alt metin sigmazsa
    /// once satirlari azalir, sonra hic basilmaz — baslik her zaman kalir.
    /// </summary>
    private void DrawTextColumn(
        SKRect area, float headlineSize, int maxHeadlineLines, bool centerVertically,
        bool withBrand = true, bool withBar = true)
    {
        var width = area.Width;
        var barHeight = _unit * 0.012f;
        var barGap = _unit * 0.03f;
        var barBlock = withBar ? barHeight + barGap : 0;

        using var brand = BrandFont(_unit * 0.03f);
        var brandLine = withBrand ? _caption.BrandLine : null;
        var brandBlock = brandLine is { Length: > 0 } ? brand.Size * 2.2f : 0;

        var available = area.Height - barBlock - brandBlock;

        using var headline = SkiaText.Fit(
            _caption.Headline, headlineSize, _unit * 0.038f, width,
            maxLines: maxHeadlineLines, maxHeight: available, bold: true);

        var sublineGap = _unit * 0.028f;
        var sublineRoom = available - headline.Height - sublineGap;
        using var subline = _caption.Subline is { Length: > 0 } sub && sublineRoom > _unit * 0.05f
            ? SkiaText.Fit(sub, _unit * 0.036f, _unit * 0.028f, width,
                maxLines: _landscape ? 4 : 3, maxHeight: sublineRoom, leading: 1.3f)
            : null;

        var blockHeight = barBlock + headline.Height + (subline is null ? 0 : sublineGap + subline.Height);
        var y = centerVertically
            ? area.Top + Math.Max(0, (available + barBlock - blockHeight) / 2)
            : area.Top;

        if (withBar)
        {
            Bar(area.Left, y, _unit * 0.09f, barHeight, _palette.Accent);
            y += barBlock;
        }

        y = headline.Draw(_canvas, area.Left, y, SKColors.White);

        if (subline is not null)
            subline.Draw(_canvas, area.Left, y + sublineGap, Translucent);

        if (brandLine is { Length: > 0 })
            DrawBrand(brandLine, area.Left, area.Bottom, brand, Translucent, _palette.Accent);
    }

    /// <summary>Alt kenarda ince cizgi ve marka satiri; cizginin y konumunu doner.</summary>
    private float DrawFooter(float margin, SKFont brand)
    {
        var baseline = _height - margin;
        var line = baseline - (brand.Size * 2.1f);

        if (_caption.BrandLine is not { Length: > 0 } brandLine) return baseline;

        Fill(SKRect.Create(margin, line, _width - (2 * margin), Math.Max(1, _unit * 0.002f)), SKColors.White.WithAlpha(60));
        DrawBrand(brandLine, margin, baseline, brand, Translucent, _palette.Accent);
        return line;
    }

    /// <summary>Basinda vurgu noktasi olan marka satiri; <paramref name="baseline"/> taban cizgisidir.</summary>
    private void DrawBrand(string text, float x, float baseline, SKFont font, SKColor color, SKColor dot)
    {
        var radius = font.Size * 0.24f;

        using var paint = new SKPaint { IsAntialias = true, Color = dot };
        _canvas.DrawCircle(x + radius, baseline - (font.Size * 0.36f), radius, paint);

        paint.Color = color;
        _canvas.DrawText(text, x + (radius * 2) + (font.Size * 0.45f), baseline, SKTextAlign.Left, font, paint);
    }

    /// <summary>Kaynagi hedef dikdortgene ortadan kirpip doldurur (CSS object-fit: cover).</summary>
    private void DrawCover(SKRect target)
    {
        var sourceRatio = (float)_source.Width / _source.Height;
        var targetRatio = target.Width / target.Height;

        var crop = sourceRatio > targetRatio
            ? SKRect.Create((_source.Width - (_source.Height * targetRatio)) / 2, 0, _source.Height * targetRatio, _source.Height)
            : SKRect.Create(0, (_source.Height - (_source.Width / targetRatio)) / 2, _source.Width, _source.Width / targetRatio);

        using var paint = new SKPaint { IsAntialias = true };
        _canvas.DrawImage(_source, crop, target, new SKSamplingOptions(SKCubicResampler.Mitchell), paint);
    }

    /// <summary>Marka karti zaten tasarimin zeminidir — oldugu gibi cizilir.</summary>
    private void DrawRaw() => _canvas.DrawImage(_source, 0, 0, SKSamplingOptions.Default);

    private void DrawGradientBackground()
    {
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(_width, _height),
            [_palette.Deep, _palette.Dark], [0f, 1f], SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader };
        _canvas.DrawRect(SKRect.Create(0, 0, _width, _height), paint);
    }

    /// <summary>
    /// Duz kart zeminine desen. Desen alan adindan secilir: bir sitenin afisleri tutarli,
    /// farkli sitelerinki ayirt edilir.
    /// </summary>
    private void DrawPattern()
    {
        var kind = _caption.ColorSeed is { Length: > 0 } seed ? (int)DesignPalette.HueOf(seed) % 3 : 0;

        using var paint = new SKPaint { IsAntialias = true };

        switch (kind)
        {
            case 0:
                // Sag ustte nokta izgarasi.
                paint.Color = SKColors.White.WithAlpha(46);
                var step = _unit * 0.036f;
                var dot = _unit * 0.0055f;
                for (var row = 0; row < 7; row++)
                {
                    for (var col = 0; col < 7; col++)
                        _canvas.DrawCircle(_width - (_unit * 0.08f) - (col * step), (_unit * 0.08f) + (row * step), dot, paint);
                }
                break;

            case 1:
                // Sag alt koseden yayilan halkalar.
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = _unit * 0.012f;
                paint.Color = SKColors.White.WithAlpha(26);
                foreach (var ratio in new[] { 0.32f, 0.5f, 0.68f })
                    _canvas.DrawCircle(_width, _height, _unit * ratio, paint);
                break;

            default:
                // Sag ust ucgende capraz seritler.
                using (var builder = new SKPathBuilder())
                {
                    builder.MoveTo(_width * 0.45f, 0);
                    builder.LineTo(_width, 0);
                    builder.LineTo(_width, _height * 0.55f);
                    builder.Close();
                    using var path = builder.Detach();

                    _canvas.Save();
                    _canvas.ClipPath(path, SKClipOperation.Intersect, antialias: true);
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = _unit * 0.018f;
                    paint.Color = SKColors.White.WithAlpha(20);
                    for (float offset = -_height; offset < _width + _height; offset += _unit * 0.06f)
                        _canvas.DrawLine(offset, 0, offset + _height, _height, paint);
                    _canvas.Restore();
                }
                break;
        }
    }

    private void Bar(float x, float y, float width, float height, SKColor color)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = color };
        _canvas.DrawRoundRect(SKRect.Create(x, y, width, height), height / 2, height / 2, paint);
    }

    private void Fill(SKRect rect, SKColor color)
    {
        using var paint = new SKPaint { Color = color };
        _canvas.DrawRect(rect, paint);
    }

    private static SKFont BrandFont(float size) =>
        new(EmbeddedFonts.Sans) { Size = size, Embolden = true, Subpixel = true };
}
