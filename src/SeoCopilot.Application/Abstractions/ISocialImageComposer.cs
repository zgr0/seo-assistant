namespace SeoCopilot.Application.Abstractions;

/// <summary>
/// Uretilen gorselden gonderi tasarimi cikarir: sablona gore baslik, alt metin ve marka satiri
/// basar. Yazi modele cizdirilmez — Turkce glifler bozuluyor ve yerlesim kontrol edilemiyor.
/// </summary>
public interface ISocialImageComposer
{
    /// <summary>
    /// Ayni baytlardan tasarlanmis bir kopya uretir; kaynak gorsel degismez. Basarisiz olursa
    /// null doner (cagiran taraf ham gorselle devam eder).
    /// </summary>
    ComposedImage? Compose(byte[] image, ImageCaption caption);
}

/// <summary>Gonderi gorselinin yerlesimi.</summary>
public enum ImageTemplate
{
    /// <summary>Tam ekran fotograf, alt kisimda gecisli perde ve baslik.</summary>
    Overlay,

    /// <summary>Fotograf bir yarida, digerinde renkli panel uzerinde baslik ve alt metin.</summary>
    Split,

    /// <summary>Renkli zeminde yuvarlak koseli, golgeli fotograf; altinda/yaninda metin.</summary>
    Framed,

    /// <summary>Tam ekran fotograf, uzerinde beyaz etiket kartinda baslik.</summary>
    Label,

    /// <summary>Fotografsiz: desenli marka zemini uzerinde buyuk baslik.</summary>
    Poster,

    /// <summary>Fotografsiz: sayfadan bir cumle alinti olarak, baslik kaynak satiri olarak.</summary>
    Quote
}

/// <param name="Headline">Gorsele basilan kisa baslik.</param>
/// <param name="BrandLine">Alt satir — marka adi ya da alan adi; bos olabilir.</param>
/// <param name="Subline">Basligi tamamlayan kisa aciklama; yalniz bazi sablonlar basar.</param>
/// <param name="Template">Yerlesim; verilmezse klasik perde + baslik.</param>
/// <param name="ColorSeed">
/// Renk tohumu (ör. alan adi). Fotografin baskin rengi okunamazsa ayni site hep ayni renkleri alir.
/// </param>
public record ImageCaption(
    string Headline,
    string? BrandLine,
    string? Subline = null,
    ImageTemplate Template = ImageTemplate.Overlay,
    string? ColorSeed = null);

/// <param name="Width">Gercek piksel genisligi — kaynak gorselden okunur.</param>
public record ComposedImage(byte[] Content, string ContentType, int Width, int Height);
