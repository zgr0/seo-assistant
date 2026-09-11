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
/// Gorsele basilan yazi: metnin secimi (<see cref="ImageCaptionBuilder"/>) ve cizimi
/// (<see cref="SkiaImageComposer"/>). Ikisi de yerel, ucretsiz ve deterministik.
/// </summary>
public class ImageCaptionTests
{
    private static SkiaImageComposer NewComposer(ImageOverlayOptions? options = null) =>
        new(Options.Create(options ?? new ImageOverlayOptions()),
            NullLogger<SkiaImageComposer>.Instance);

    /// <summary>Tek renkli test gorseli — cizim oncesi/sonrasi fark olculebilsin diye duz.</summary>
    private static byte[] SolidImage(SKColor color, int size = 512)
    {
        using var bitmap = new SKBitmap(size, size);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(color);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 92);
        return data.ToArray();
    }

    // --- metin secimi ---

    [Fact]
    public void Headline_comes_from_h1_and_brand_line_from_the_profile()
    {
        var variant = new ContentVariant { Body = "Gövde metni" };
        var page = new Page { Url = "https://ornek.com/x", H1Texts = ["Plastik enjeksiyon makinaları."] };
        var brand = new BrandProfile { Name = "Örnek Makina" };

        var caption = ImageCaptionBuilder.Build(variant, page, brand);

        Assert.NotNull(caption);
        // Sondaki noktalama gorselde gereksiz.
        Assert.Equal("Plastik enjeksiyon makinaları", caption.Headline);
        Assert.Equal("Örnek Makina", caption.BrandLine);
    }

    [Fact]
    public void Brand_line_falls_back_to_the_host_without_www()
    {
        var caption = ImageCaptionBuilder.Build(
            new ContentVariant { Body = "x" },
            new Page { Url = "https://www.generalmakina.com.tr/urun", H1Texts = ["Enjeksiyon makinaları"] },
            brand: null);

        Assert.Equal("generalmakina.com.tr", caption!.BrandLine);
    }

    [Fact]
    public void Navigational_page_names_are_not_printed_on_the_image()
    {
        var page = new Page
        {
            Url = "https://ornek.com/hakkimizda",
            H1Texts = ["Hakkımızda"],
            Title = "Hakkımızda | Örnek Makina",
            MetaDescription = "20 yılı aşkın süredir plastik enjeksiyon makinaları üretiyoruz."
        };

        var caption = ImageCaptionBuilder.Build(new ContentVariant { Body = "x" }, page, null);

        Assert.NotNull(caption);
        Assert.DoesNotContain("Hakkımızda", caption.Headline);
        Assert.StartsWith("20 yılı aşkın", caption.Headline);
    }

    [Fact]
    public void Nothing_meaningful_means_no_text_at_all()
    {
        var page = new Page { Url = "https://ornek.com/iletisim", H1Texts = ["İletişim"] };

        // Govde de etiketten ibaretse gorsel yazisiz kalir — anlamsiz yazi basmaktansa hic basma.
        Assert.Null(ImageCaptionBuilder.Build(new ContentVariant { Body = "İletişim" }, page, null));
    }

    [Fact]
    public void Title_is_trimmed_to_its_first_segment_and_body_is_the_last_resort()
    {
        var fromTitle = ImageCaptionBuilder.Build(
            new ContentVariant { Body = "x" },
            new Page { Url = "https://ornek.com", Title = "Servo enjeksiyon makinaları | Örnek | Anasayfa" },
            null);
        Assert.Equal("Servo enjeksiyon makinaları", fromTitle!.Headline);

        var fromBody = ImageCaptionBuilder.Build(
            new ContentVariant { Body = "İlk satır kanca\n\nGövde devamı" }, page: null, brand: null);
        Assert.Equal("İlk satır kanca", fromBody!.Headline);
    }

    [Fact]
    public void Long_headlines_are_clipped_on_a_word_boundary()
    {
        var caption = ImageCaptionBuilder.Build(
            new ContentVariant { Body = "x" },
            new Page { Url = "https://ornek.com", H1Texts = [string.Join(' ', Enumerable.Repeat("kelime", 30))] },
            null);

        Assert.True(caption!.Headline.Length <= ImageCaptionBuilder.MaxHeadlineChars + 1);
        Assert.EndsWith("…", caption.Headline);
        Assert.DoesNotContain("kelim…", caption.Headline); // kelime ortasindan kesilmez
    }

    // --- cizim ---

    [Fact]
    public void Composed_image_keeps_its_dimensions_and_draws_over_the_bottom()
    {
        var source = SolidImage(new SKColor(40, 60, 90));

        var result = NewComposer().Compose(source, new ImageCaption("Başlık", "Marka"));

        Assert.NotNull(result);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.Equal(512, result.Width);
        Assert.Equal(512, result.Height);

        using var composed = SKBitmap.Decode(result.Content);
        // Ust kisim dokunulmadan kalir, alt kisim perde + yazi ile degisir.
        Assert.True(RowDelta(composed, new SKColor(40, 60, 90), 20) < 8);
        Assert.True(RowDelta(composed, new SKColor(40, 60, 90), 500) > 20);
    }

    [Fact]
    public void Turkish_glyphs_are_rendered_not_dropped()
    {
        var source = SolidImage(new SKColor(30, 30, 30));
        var composer = NewComposer();

        var withTurkish = composer.Compose(source, new ImageCaption("İĞŞÇÖÜ ığşçöü", null))!;
        var blank = composer.Compose(source, new ImageCaption(" ", null))!;

        using var a = SKBitmap.Decode(withTurkish.Content);
        using var b = SKBitmap.Decode(blank.Content);

        // Glifler eksik olsaydi iki gorsel neredeyse ayni olurdu.
        Assert.True(Difference(a, b) > 1.0, "Türkçe glifler çizilmemiş görünüyor");
    }

    [Fact]
    public void Bright_images_get_a_light_scrim_and_dark_text()
    {
        var composer = NewComposer();
        var bright = composer.Compose(SolidImage(SKColors.White), new ImageCaption("Başlık", null))!;
        var dark = composer.Compose(SolidImage(SKColors.Black), new ImageCaption("Başlık", null))!;

        using var brightBitmap = SKBitmap.Decode(bright.Content);
        using var darkBitmap = SKBitmap.Decode(dark.Content);

        // Acik gorselde alt bolge acik kalir; koyu gorselde koyu kalir — perde rengi uyum saglar.
        Assert.True(Luma(brightBitmap, 500) > 0.6);
        Assert.True(Luma(darkBitmap, 500) < 0.4);
    }

    [Fact]
    public void Disabled_overlay_returns_null_so_the_raw_image_is_used()
    {
        var composer = NewComposer(new ImageOverlayOptions { Enabled = false });

        Assert.Null(composer.Compose(SolidImage(SKColors.Gray), new ImageCaption("Başlık", null)));
    }

    [Fact]
    public void Broken_bytes_do_not_throw()
    {
        Assert.Null(NewComposer().Compose([1, 2, 3, 4], new ImageCaption("Başlık", null)));
    }

    // --- yardimcilar ---

    /// <summary>Bir satirin beklenen renkten ortalama sapmasi (0-255).</summary>
    private static double RowDelta(SKBitmap bitmap, SKColor expected, int y)
    {
        double total = 0;
        for (var x = 0; x < bitmap.Width; x += 4)
        {
            var p = bitmap.GetPixel(x, y);
            total += (Math.Abs(p.Red - expected.Red)
                + Math.Abs(p.Green - expected.Green)
                + Math.Abs(p.Blue - expected.Blue)) / 3.0;
        }

        return total / (bitmap.Width / 4.0);
    }

    /// <summary>Iki gorselin ortalama piksel farki.</summary>
    private static double Difference(SKBitmap a, SKBitmap b)
    {
        double total = 0;
        var samples = 0;

        for (var y = 0; y < a.Height; y += 4)
        {
            for (var x = 0; x < a.Width; x += 4)
            {
                var p = a.GetPixel(x, y);
                var q = b.GetPixel(x, y);
                total += (Math.Abs(p.Red - q.Red) + Math.Abs(p.Green - q.Green) + Math.Abs(p.Blue - q.Blue)) / 3.0;
                samples++;
            }
        }

        return total / samples;
    }

    private static double Luma(SKBitmap bitmap, int y)
    {
        double total = 0;
        var samples = 0;

        for (var x = 0; x < bitmap.Width; x += 4)
        {
            var p = bitmap.GetPixel(x, y);
            total += (0.299 * p.Red + 0.587 * p.Green + 0.114 * p.Blue) / 255.0;
            samples++;
        }

        return total / samples;
    }
}
