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
}
