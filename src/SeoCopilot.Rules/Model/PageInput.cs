namespace SeoCopilot.Rules.Model;

/// <summary>
/// Kural motorunun tek girdi tipi. Application.ExtractedPage'den bagimsiz tutuldu ki
/// bu proje saf kalsin (Domain disinda referans yok).
/// </summary>
public sealed record PageInput
{
    public required string Url { get; init; }
    public int StatusCode { get; init; }
    public string? Title { get; init; }
    public string? MetaDescription { get; init; }
    public IReadOnlyList<string> H1 { get; init; } = [];
    public IReadOnlyList<string> InternalLinks { get; init; } = [];
    public int WordCount { get; init; }
    public bool HasCanonical { get; init; }

    /// <summary>rel=canonical'in mutlak hali; kendini isaret etmiyorsa bulgu uretir.</summary>
    public string? CanonicalUrl { get; init; }

    /// <summary>Sayfaya varmak icin izlenen yonlendirme sayisi.</summary>
    public int RedirectCount { get; init; }

    /// <summary>
    /// Govde baska bir adresten geldi mi. true ise bu URL'in kendi icerigi yoktur; icerik
    /// kurallari yonlendirmenin <em>hedefinde</em> degerlendirilir, burada degil — yoksa ayni
    /// belge iki kez raporlanir ve canonical kendi adresini gostermiyor sanilir.
    /// </summary>
    public bool IsRedirect { get; init; }

    /// <summary>Yonlendirmenin izlenemeyen http disi hedefi (orn. <c>javascript:;</c>).</summary>
    public string? InvalidRedirectTarget { get; init; }

    /// <summary>Gecersiz bicimli ld+json blogu sayisi — isaretleme var ama okunamiyor.</summary>
    public int InvalidSchemaBlocks { get; init; }

    /// <summary>meta[name=robots] + X-Robots-Tag birlesimi.</summary>
    public string? RobotsMeta { get; init; }

    /// <summary>Govdedeki basliklarin belge sirasindaki seviyeleri (h1 → 1, h2 → 2 ...).</summary>
    public IReadOnlyList<int> HeadingLevels { get; init; } = [];

    /// <summary>Ic linklerin anchor metinleri; bos/null olanlar da yer alir.</summary>
    public IReadOnlyList<string?> InternalAnchorTexts { get; init; } = [];

    public int ImagesTotal { get; init; }
    public int ImagesNoAlt { get; init; }

    /// <summary>Boyutu olculebilen gorseller. Olcum yapilmadiysa bos — kural sessiz kalir.</summary>
    public IReadOnlyList<ImageSize> ImageSizes { get; init; } = [];

    /// <summary>JSON-LD / microdata icinden toplanan schema.org tipleri.</summary>
    public IReadOnlyList<string> SchemaTypes { get; init; } = [];

    /// <summary>Sayfada bulunan Open Graph ozellik adlari (orn. "og:title").</summary>
    public IReadOnlyList<string> OgTags { get; init; } = [];

    /// <summary>&lt;html lang&gt; degeri.</summary>
    public string? Lang { get; init; }
}

/// <summary>Tek bir gorselin olculmus indirme boyutu.</summary>
public sealed record ImageSize(string Url, long Bytes);
