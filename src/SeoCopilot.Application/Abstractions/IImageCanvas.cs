namespace SeoCopilot.Application.Abstractions;

/// <summary>
/// Ucretsiz gorsel kaynaklari icin cizim: sitenin kendi fotografini platform oranina
/// kirpar ya da fotograf yoksa marka karti cizer. Uretim API'si gerektirmez.
/// </summary>
public interface IImageCanvas
{
    /// <summary>
    /// Kaynak gorseli oranina gore ortadan kirpip olcekler. Cozulemeyen, cok kucuk ya da
    /// fotograf olamayacak kadar uzun/ince (logo, serit) gorsellerde null.
    /// </summary>
    GeneratedImage? Fit(byte[] source, string aspectRatio);

    /// <summary>
    /// Duz renk gecisli marka karti. Ayni <paramref name="seed"/> (ör. alan adi) hep ayni
    /// renkleri verir — bir sitenin kartlari birbirine benzesin.
    /// </summary>
    GeneratedImage Card(string aspectRatio, string seed);
}
