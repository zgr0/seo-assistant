using System.Security.Cryptography;
using System.Text;

namespace SeoCopilot.Application.Common;

/// <summary>
/// URL kanoniklestirme. Frontier'da tekrarli ziyaret olmasin ve pages.url_hash
/// tutarli kalsin diye tek kaynak burasi. Saf ve bagimliliksiz.
/// </summary>
public static class UrlNormalizer
{
    /// <summary>Kanonikligi bozmayan, yalniz takip amacli parametreler.</summary>
    private static readonly HashSet<string> TrackingParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "fbclid", "gclid", "dclid", "msclkid", "yclid", "twclid",
        "mc_cid", "mc_eid", "_ga", "_gl", "igshid", "ref_src"
    };

    /// <summary>
    /// HTML sayfasi degil, indirilebilir varlik olduguna isaret eden uzantilar.
    /// Bunlar taranmaz; yalniz durum yoklamasi yapilir (kirik link tespiti icin).
    /// </summary>
    private static readonly HashSet<string> AssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".svg", ".ico", ".bmp", ".tif", ".tiff",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".csv", ".rtf", ".odt", ".ods", ".odp",
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2",
        ".mp3", ".mp4", ".m4a", ".avi", ".mov", ".wmv", ".webm", ".ogg", ".ogv", ".wav", ".flac",
        ".woff", ".woff2", ".ttf", ".eot", ".otf",
        ".css", ".js", ".mjs", ".map", ".json", ".xml", ".rss", ".atom", ".txt",
        ".exe", ".dmg", ".apk", ".msi", ".pkg", ".deb", ".rpm"
    };

    /// <summary>Uzantisina bakarak URL'in indirilebilir bir varlik olup olmadigini kestirir.</summary>
    public static bool IsLikelyAsset(Uri url)
    {
        var path = url.AbsolutePath;
        var slash = path.LastIndexOf('/');
        var name = slash < 0 ? path : path[(slash + 1)..];

        var dot = name.LastIndexOf('.');
        if (dot < 0 || dot == name.Length - 1) return false;

        return AssetExtensions.Contains(name[dot..]);
    }

    /// <summary>Goreli veya mutlak href'i, base'e gore kanonik mutlak URL'e cevirir.</summary>
    public static bool TryNormalize(string? href, Uri baseUri, out Uri result)
    {
        result = null!;
        if (string.IsNullOrWhiteSpace(href)) return false;

        var trimmed = href.Trim();
        if (trimmed.Length == 0 || trimmed[0] == '#') return false;

        if (!Uri.TryCreate(baseUri, trimmed, out var absolute)) return false;
        if (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps) return false;
        if (string.IsNullOrEmpty(absolute.Host)) return false;

        var builder = new UriBuilder(absolute)
        {
            Scheme = absolute.Scheme.ToLowerInvariant(),
            Host = absolute.Host.ToLowerInvariant(),
            Fragment = string.Empty,
            Query = StripTrackingParams(absolute.Query)
        };

        if ((builder.Scheme == Uri.UriSchemeHttp && builder.Port == 80) ||
            (builder.Scheme == Uri.UriSchemeHttps && builder.Port == 443))
            builder.Port = -1;

        if (string.IsNullOrEmpty(builder.Path)) builder.Path = "/";

        result = builder.Uri;
        return true;
    }

    /// <summary>Mutlak URL'i kanoniklestirir.</summary>
    public static bool TryNormalize(string? url, out Uri result)
    {
        result = null!;
        return Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var absolute)
            && TryNormalize(absolute.AbsoluteUri, absolute, out result);
    }

    /// <summary>Ayni siteye mi ait — bastaki www. yok sayilir.</summary>
    public static bool IsInternal(Uri url, Uri baseUri) =>
        string.Equals(StripWww(url.Host), StripWww(baseUri.Host), StringComparison.OrdinalIgnoreCase);

    /// <summary>pages.url_hash — b-tree index uzunluk limiti icin sha256(url).</summary>
    public static byte[] Hash(string url) => SHA256.HashData(Encoding.UTF8.GetBytes(url));

    public static byte[] Hash(Uri url) => Hash(url.AbsoluteUri);

    /// <summary>
    /// sites.base_url icin kok URL uretir: sema + host (+ varsayilan olmayan port),
    /// sonda / yok. Gecersizse null.
    /// </summary>
    public static string? NormalizeSiteBaseUrl(string? baseUrl)
    {
        if (!Uri.TryCreate(baseUrl?.Trim(), UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
        if (string.IsNullOrEmpty(uri.Host)) return null;

        var scheme = uri.Scheme.ToLowerInvariant();
        var host = uri.Host.ToLowerInvariant();
        var isDefaultPort = (scheme == Uri.UriSchemeHttp && uri.Port == 80)
            || (scheme == Uri.UriSchemeHttps && uri.Port == 443);

        var root = isDefaultPort ? $"{scheme}://{host}" : $"{scheme}://{host}:{uri.Port}";
        var path = uri.AbsolutePath.TrimEnd('/');
        return path.Length == 0 ? root : root + path;
    }

    private static string StripWww(string host) =>
        host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;

    private static string StripTrackingParams(string query)
    {
        if (query.Length <= 1) return string.Empty;

        var kept = new List<string>();
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            var key = eq < 0 ? pair : pair[..eq];
            if (key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase)) continue;
            if (TrackingParams.Contains(key)) continue;
            kept.Add(pair);
        }

        return kept.Count == 0 ? string.Empty : string.Join('&', kept);
    }
}
