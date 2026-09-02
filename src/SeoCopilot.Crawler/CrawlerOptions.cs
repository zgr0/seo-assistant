namespace SeoCopilot.Crawler;

/// <summary>appsettings "Crawler" bolumu.</summary>
public sealed class CrawlerOptions
{
    public const string Section = "Crawler";

    /// <summary>Giden isteklerin User-Agent basligi.</summary>
    public string UserAgent { get; set; } = "SeoCopilotBot/1.0 (+https://seocopilot.local/bot)";

    /// <summary>robots.txt icinde bizi hedefleyen token (kucuk harf).</summary>
    public string UserAgentToken { get; set; } = "seocopilotbot";

    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>Tek bir URL icin izlenecek azami yonlendirme sayisi.</summary>
    public int MaxRedirects { get; set; } = 5;

    /// <summary>Bu boyutun uzerindeki HTML govdesi kirpilir.</summary>
    public int MaxHtmlBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>pages.main_text icin saklanacak azami karakter.</summary>
    public int MaxMainTextChars { get; set; } = 200_000;

    /// <summary>
    /// IMAGE_TOO_LARGE kurali icin sayfa basina HEAD ile boyutu olculecek azami gorsel sayisi.
    /// 0 → olcum kapali, kural sessiz kalir. Sonuclar crawl boyunca URL bazinda onbellege alinir.
    /// </summary>
    public int MaxImageChecksPerPage { get; set; } = 10;

    /// <summary>
    /// Chromium'a gecilecek ek baslatma argumanlari (yalniz RenderJs aciksa kullanilir).
    /// Konteyner icinde <c>--no-sandbox</c> gerekir: Chromium'un kendi kum havuzu ayricalikli
    /// cekirdek yetenekleri ister, tipik bir konteynerde bunlar yoktur ve tarayici acilmaz.
    /// </summary>
    public string[] BrowserArgs { get; set; } = [];
}
