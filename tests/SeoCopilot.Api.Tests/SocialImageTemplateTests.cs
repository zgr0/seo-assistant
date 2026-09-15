using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Infrastructure.Media;
using SkiaSharp;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// SkiaSharp tasarim sablonlari: secim kurali (<see cref="ImageTemplatePicker"/>), alt metin
/// secimi ve cizim. Hepsi yerel ve deterministik — gorsel API'si yok.
/// </summary>
public class SocialImageTemplateTests
{
    private static readonly SkiaImageComposer Composer = new(
        Options.Create(new ImageOverlayOptions()), NullLogger<SkiaImageComposer>.Instance);

    private static readonly SkiaImageCanvas Canvas = new(NullLogger<SkiaImageCanvas>.Instance);

    private const string Seed = "generalmakina.com.tr";

    private static ImageCaption Caption(ImageTemplate template, string? subline = "Servo motor sayesinde enerji tasarrufu ve sessiz çalışma.") =>
        new("A6 Serisi Servo Enjeksiyon Makinası", "generalmakina.com.tr", subline, template, Seed);

    // --- secim ---

    [Fact]
    public void Photos_rotate_through_photo_templates_and_cards_through_card_templates()
    {
        var photo = Enumerable.Range(0, 4)
            .Select(i => ImageTemplatePicker.Pick([], photo: true, hasSubline: true, i))
            .ToList();
        var card = Enumerable.Range(0, 2)
            .Select(i => ImageTemplatePicker.Pick([], photo: false, hasSubline: true, i))
            .ToList();

        Assert.Equal(ImageTemplatePicker.PhotoTemplates, photo);
        Assert.Equal(ImageTemplatePicker.CardTemplates, card);
    }

    [Fact]
    public void Quote_needs_a_subline_and_the_allow_list_is_respected()
    {
        Assert.Equal(ImageTemplate.Poster, ImageTemplatePicker.Pick([], photo: false, hasSubline: false, index: 1));

        ImageTemplate[] onlySplit = [ImageTemplate.Split];
        Assert.All(Enumerable.Range(0, 5), i =>
            Assert.Equal(ImageTemplate.Split, ImageTemplatePicker.Pick(onlySplit, photo: true, hasSubline: true, i)));

        // Fotograf bulunamasa da kullanicinin sectigi fotografli sablona uyulur (kart zemininde).
        Assert.Equal(ImageTemplate.Split, ImageTemplatePicker.Pick(onlySplit, photo: false, hasSubline: true, 0));

        // Tek secenek alintiydi ama alt metin yok: afise duser.
        Assert.Equal(ImageTemplate.Poster, ImageTemplatePicker.Pick([ImageTemplate.Quote], photo: false, hasSubline: false, 0));
    }

    [Fact]
    public void Template_names_are_parsed_case_insensitively_and_unknown_names_are_rejected()
    {
        Assert.Equal(
            [ImageTemplate.Split, ImageTemplate.Quote],
            ImageTemplatePicker.Parse(["split", " QUOTE ", "Split", ""]));

        Assert.Throws<InvalidOperationException>(() => ImageTemplatePicker.Parse(["Carousel"]));
        Assert.Throws<InvalidOperationException>(() => ImageTemplatePicker.Parse(["42"]));

        Assert.True(ImageTemplatePicker.WantsCardOnly([ImageTemplate.Poster, ImageTemplate.Quote]));
        Assert.False(ImageTemplatePicker.WantsCardOnly([ImageTemplate.Poster, ImageTemplate.Label]));
        Assert.False(ImageTemplatePicker.WantsCardOnly([]));
    }

    [Fact]
    public void Subline_comes_from_the_description_but_never_repeats_the_headline()
    {
        var page = new Page
        {
            Url = "https://ornek.com/a6",
            H1Texts = ["A6 servo makina"],
            MetaDescription = "Meta açıklaması: enerji tasarruflu servo motorlu makina."
        };

        var fromDescription = ImageCaptionBuilder.Build(
            new ContentVariant { Body = "x", Description = "Yüzde kırka varan enerji tasarrufu sağlar." }, page, null);
        Assert.Equal("Yüzde kırka varan enerji tasarrufu sağlar", fromDescription!.Subline);

        // Aciklama basligin tekrariysa meta description'a gecilir.
        var repeated = ImageCaptionBuilder.Build(
            new ContentVariant { Body = "x", Description = "A6 servo makina hakkında her şey" }, page, null);
        Assert.StartsWith("Meta açıklaması", repeated!.Subline);

        var none = ImageCaptionBuilder.Build(
            new ContentVariant { Body = "x", Description = "kısa" }, new Page { Url = "https://ornek.com", H1Texts = ["Başlık"] }, null);
        Assert.Null(none!.Subline);
    }

    // --- cizim ---

    [Theory]
    [InlineData("1:1", 1080, 1080)]
    [InlineData("16:9", 1200, 675)]
    public void Every_template_renders_at_the_source_size_and_looks_different(string aspect, int width, int height)
    {
        var photo = Photo(aspect);
        var card = Canvas.Card(aspect, Seed).Content;

        var rendered = new Dictionary<ImageTemplate, SKBitmap>();
        foreach (var template in Enum.GetValues<ImageTemplate>())
        {
            var source = ImageTemplatePicker.CardTemplates.Contains(template) ? card : photo;
            var result = Composer.Compose(source, Caption(template));

            Assert.NotNull(result);
            Assert.Equal(width, result.Width);
            Assert.Equal(height, result.Height);
            rendered[template] = SKBitmap.Decode(result.Content);
        }

        // Ayni zemindeki sablonlar birbirinden gozle ayirt edilecek kadar farkli.
        foreach (var group in new[] { ImageTemplatePicker.PhotoTemplates, ImageTemplatePicker.CardTemplates })
        {
            for (var i = 0; i < group.Length; i++)
            {
                for (var j = i + 1; j < group.Length; j++)
                {
                    Assert.True(Difference(rendered[group[i]], rendered[group[j]]) > 6,
                        $"{group[i]} ve {group[j]} neredeyse aynı");
                }
            }
        }

        foreach (var bitmap in rendered.Values) bitmap.Dispose();
    }

    [Fact]
    public void Split_keeps_the_photo_on_one_side_and_draws_a_panel_on_the_other()
    {
        var red = new SKColor(200, 40, 40);
        var result = Composer.Compose(Solid(red, 1200, 675), Caption(ImageTemplate.Split))!;

        using var bitmap = SKBitmap.Decode(result.Content);

        // Yatayda fotograf solda: sol ceyrek kaynakla ayni, sag panel ayni renk ailesinde ama koyu.
        Assert.True(ColorDelta(bitmap.GetPixel(150, 330), red) < 12);
        var panel = bitmap.GetPixel(1150, 20);
        Assert.True(ColorDelta(panel, red) > 30);
        Assert.True(panel.Red > panel.Green && panel.Red > panel.Blue, "Panel fotoğrafın tonunu almalı");
        Assert.True(Luma(panel) < 0.35, "Panel beyaz yazının okunacağı kadar koyu olmalı");
    }

    [Fact]
    public void Label_leaves_the_photo_full_bleed_and_puts_a_light_card_at_the_bottom()
    {
        var blue = new SKColor(40, 90, 180);
        var result = Composer.Compose(Solid(blue, 1080, 1080), Caption(ImageTemplate.Label))!;

        using var bitmap = SKBitmap.Decode(result.Content);

        Assert.True(ColorDelta(bitmap.GetPixel(540, 60), blue) < 12);
        // Kartin sag ic kenari: metin yok, beyaz zemin.
        Assert.True(Luma(bitmap.GetPixel(1000, 1000)) > 0.85);
    }

    [Fact]
    public void Quote_without_a_subline_falls_back_to_the_poster()
    {
        var card = Canvas.Card("1:1", Seed).Content;

        using var quote = SKBitmap.Decode(Composer.Compose(card, Caption(ImageTemplate.Quote, subline: null))!.Content);
        using var poster = SKBitmap.Decode(Composer.Compose(card, Caption(ImageTemplate.Poster, subline: null))!.Content);

        Assert.True(Difference(quote, poster) < 0.5);
    }

    [Fact]
    public void Panel_color_follows_the_photo_and_colorless_photos_use_the_site_seed()
    {
        using var fromRed = Panel(Solid(new SKColor(200, 40, 40), 1200, 675), Seed);
        using var fromBlue = Panel(Solid(new SKColor(40, 60, 200), 1200, 675), Seed);
        Assert.True(ColorDelta(fromRed.GetPixel(1150, 20), fromBlue.GetPixel(1150, 20)) > 20);

        // Gri fotografta renk tohumdan gelir: ayni site hep ayni, farkli site farkli.
        var gray = Solid(new SKColor(128, 128, 128), 1200, 675);
        using var siteA = Panel(gray, "a-sitesi.com");
        using var siteAgain = Panel(gray, "a-sitesi.com");
        using var siteB = Panel(gray, "baska-bir-site.net");

        Assert.True(ColorDelta(siteA.GetPixel(1150, 20), siteAgain.GetPixel(1150, 20)) < 3);
        Assert.True(ColorDelta(siteA.GetPixel(1150, 20), siteB.GetPixel(1150, 20)) > 20);
    }

    [Fact]
    public void Very_long_texts_stay_inside_the_image_without_throwing()
    {
        var longText = string.Join(' ', Enumerable.Repeat("Çokuzunkelimeliİstanbulbaşlığı ve devamı", 20));
        var caption = new ImageCaption(longText, longText, longText, ImageTemplate.Split, Seed);

        foreach (var template in Enum.GetValues<ImageTemplate>())
        {
            var result = Composer.Compose(Photo("16:9"), caption with { Template = template });
            Assert.NotNull(result);
        }
    }

    // --- yardimcilar ---

    private static SKBitmap Panel(byte[] source, string seed) =>
        SKBitmap.Decode(Composer.Compose(source, Caption(ImageTemplate.Split) with { ColorSeed = seed })!.Content);

    /// <summary>Renkli, desenli sentetik fotograf — site fotografi yolundan gecirilir.</summary>
    private static byte[] Photo(string aspect)
    {
        using var bitmap = new SKBitmap(1600, 1200);
        using (var canvas = new SKCanvas(bitmap))
        {
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(1600, 1200),
                [new SKColor(20, 110, 160), new SKColor(230, 150, 40)], [0f, 1f], SKShaderTileMode.Clamp);
            using var paint = new SKPaint { Shader = shader };
            canvas.DrawRect(0, 0, 1600, 1200, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return Canvas.Fit(data.ToArray(), aspect)!.Content;
    }

    private static byte[] Solid(SKColor color, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(color);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
        return data.ToArray();
    }

    private static double ColorDelta(SKColor a, SKColor b) =>
        (Math.Abs(a.Red - b.Red) + Math.Abs(a.Green - b.Green) + Math.Abs(a.Blue - b.Blue)) / 3.0;

    private static double Luma(SKColor p) => (0.299 * p.Red + 0.587 * p.Green + 0.114 * p.Blue) / 255.0;

    private static double Difference(SKBitmap a, SKBitmap b)
    {
        double total = 0;
        var samples = 0;

        for (var y = 0; y < a.Height; y += 6)
        {
            for (var x = 0; x < a.Width; x += 6)
            {
                total += ColorDelta(a.GetPixel(x, y), b.GetPixel(x, y));
                samples++;
            }
        }

        return total / samples;
    }
}
