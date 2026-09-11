namespace SeoCopilot.Application.Abstractions;

/// <summary>
/// Uretilen gorselin uzerine baslik ve marka satiri basar. Yazi modele cizdirilmez —
/// Turkce glifler bozuluyor ve yerlesim kontrol edilemiyor.
/// </summary>
public interface ISocialImageComposer
{
    /// <summary>
    /// Ayni baytlardan yazili bir kopya uretir; kaynak gorsel degismez. Basarisiz olursa
    /// null doner (cagiran taraf ham gorselle devam eder).
    /// </summary>
    ComposedImage? Compose(byte[] image, ImageCaption caption);
}

/// <param name="Headline">Gorsele basilan kisa baslik.</param>
/// <param name="BrandLine">Alt satir — marka adi ya da alan adi; bos olabilir.</param>
public record ImageCaption(string Headline, string? BrandLine);

/// <param name="Width">Gercek piksel genisligi — kaynak gorselden okunur.</param>
public record ComposedImage(byte[] Content, string ContentType, int Width, int Height);
