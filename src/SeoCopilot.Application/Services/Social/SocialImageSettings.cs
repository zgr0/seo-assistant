using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Application.Services.Social;

/// <summary>Gonderi gorselinin nereden gelecegi ve nasil tasarlanacagi.</summary>
public sealed class SocialImageSettings
{
    public const string Section = "SocialImages";

    public const string SiteSource = "site";
    public const string AiSource = "ai";
    public const string CardSource = "card";

    /// <summary>
    /// Denenecek kaynaklar, sirasiyla; ilk basarili olan kullanilir.
    /// <list type="bullet">
    /// <item><c>site</c> — sayfanin kendi fotograflari (ucretsiz)</item>
    /// <item><c>ai</c> — yapay zeka uretimi (FLUX; kredi ister)</item>
    /// <item><c>card</c> — marka karti (ucretsiz, hic basarisiz olmaz)</item>
    /// </list>
    /// Varsayilan ucretsizdir; <c>ai</c> yalniz acikca eklenirse cagrilir.
    /// </summary>
    public List<string> Sources { get; set; } = [SiteSource, CardSource];

    /// <summary>
    /// Kullanilacak tasarim sablonlari (bkz. <see cref="ImageTemplate"/>). Bos ise hepsi
    /// donusumlu kullanilir; ör. yalniz <c>Overlay</c> verilirse eski tek tip gorunum kalir.
    /// </summary>
    public List<ImageTemplate> Templates { get; set; } = [];
}
