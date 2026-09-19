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
    /// Is girdisinde (<c>content_jobs.input</c>) kullanicinin istedigi kaynak. <c>"ai"</c> ise
    /// ayarlardaki sira yerine <see cref="AiFirstSources"/> kullanilir.
    /// </summary>
    public const string InputKey = "imageSource";

    /// <summary>
    /// "Yapay zeka ile uret" dugmesinin sirasi: uretim basarisiz olursa (kota, zaman asimi)
    /// sitenin fotografina, o da yoksa marka kartina dusulur.
    /// </summary>
    public static readonly IReadOnlyList<string> AiFirstSources = [AiSource, SiteSource, CardSource];

    /// <summary>
    /// Denenecek kaynaklar, sirasiyla; ilk basarili olan kullanilir.
    /// <list type="bullet">
    /// <item><c>site</c> — sayfanin kendi fotograflari (ucretsiz)</item>
    /// <item><c>ai</c> — yapay zeka uretimi (Cloudflare Workers AI; gunluk ucretsiz kota)</item>
    /// <item><c>card</c> — marka karti (ucretsiz, hic basarisiz olmaz)</item>
    /// </list>
    /// Varsayilan ucretsizdir; <c>ai</c> yalniz acikca eklenirse ya da kullanici
    /// "Yapay zeka ile uret" dugmesine basarsa cagrilir.
    /// </summary>
    public List<string> Sources { get; set; } = [SiteSource, CardSource];

    /// <summary>
    /// Kullanilacak tasarim sablonlari (bkz. <see cref="ImageTemplate"/>). Bos ise hepsi
    /// donusumlu kullanilir; ör. yalniz <c>Overlay</c> verilirse eski tek tip gorunum kalir.
    /// </summary>
    public List<ImageTemplate> Templates { get; set; } = [];

    /// <summary>
    /// Kiraci basina gunluk (UTC) yapay zeka gorseli siniri. Cloudflare'in ucretsiz kotasi
    /// hesap genelidir — tek kiraci hepsini tuketmesin.
    /// </summary>
    public int MaxAiImagesPerDay { get; set; } = 20;
}
