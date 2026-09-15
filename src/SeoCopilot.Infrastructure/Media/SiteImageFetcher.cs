using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Infrastructure.Media;

public sealed class SiteImageFetcherOptions
{
    public const string Section = "SiteImages";

    /// <summary>Tek gorsel icin azami boyut — buyuk dosya bellegi tuketmesin.</summary>
    public int MaxBytes { get; set; } = 8 * 1024 * 1024;

    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Yalniz testler icin: yerel test sitesi 127.0.0.1'de ve rastgele portta calisir.
    /// Uretimde acilmamalidir — sayfadaki bir adres ic aga istek attirabilir.
    /// </summary>
    public bool AllowPrivateNetworks { get; set; }

    public string UserAgent { get; set; } = "SeoCopilotBot/1.0";
}

/// <summary>
/// Taranan sayfadaki gorseli indirir. Adres sayfa icerigidir, yani guvenilmez: baglanti
/// kurulurken cozulen IP ic ag, geri dongu, link-local (bulut meta veri adresi 169.254.169.254
/// dahil) ya da CGNAT araligindaysa reddedilir. Kontrol DNS cozumunde degil soket
/// baglantisinda yapildigi icin DNS yeniden baglama ve ic adrese yonlendirme de engellenir.
/// </summary>
public sealed class SiteImageFetcher(
    HttpClient http, IOptions<SiteImageFetcherOptions> options, ILogger<SiteImageFetcher> logger)
    : ISiteImageFetcher
{
    private readonly SiteImageFetcherOptions _opt = options.Value;

    public async Task<byte[]?> FetchAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_opt.TimeoutSeconds));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd(_opt.UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

            using var response = await http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            if (!response.IsSuccessStatusCode) return null;

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            // Bazi sunucular gorseli octet-stream olarak doner; icerik yine de cozulerek dogrulanir.
            if (mediaType is not null
                && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                && mediaType != "application/octet-stream")
            {
                return null;
            }

            if (response.Content.Headers.ContentLength > _opt.MaxBytes) return null;

            return await ReadLimitedAsync(response.Content, _opt.MaxBytes, timeout.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Engellenen adres, zaman asimi, bozuk yanit — aday atlanir, siradakine gecilir.
            logger.LogDebug(ex, "Site görseli indirilemedi: {Url}", url);
            return null;
        }
    }

    /// <summary>Content-Length yalan soyleyebilir — okurken de sinir uygulanir.</summary>
    private static async Task<byte[]?> ReadLimitedAsync(HttpContent content, int maxBytes, CancellationToken ct)
    {
        await using var stream = await content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();

        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > maxBytes) return null;
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// HttpClient'in birincil isleyicisi: her baglantida (yonlendirmeler dahil) hedef IP'yi
    /// dogrular.
    /// </summary>
    public static SocketsHttpHandler CreateHandler(SiteImageFetcherOptions options) => new()
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 3,
        AutomaticDecompression = DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        ConnectCallback = async (context, ct) =>
        {
            var port = context.DnsEndPoint.Port;
            if (!options.AllowPrivateNetworks && port is not (80 or 443))
                throw new HttpRequestException($"Standart dışı port reddedildi: {port}");

            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            var allowed = options.AllowPrivateNetworks
                ? addresses
                : [.. addresses.Where(address => !IsNonPublic(address))];

            if (allowed.Length == 0)
                throw new HttpRequestException($"İç ağ adresine izin verilmiyor: {context.DnsEndPoint.Host}");

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(allowed, port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    };

    /// <summary>Genel internette yonlendirilemeyen ya da ozel amacli adresler.</summary>
    public static bool IsNonPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address)) return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] switch
            {
                0 or 10 or 127 => true,                    // bu ag, ozel, geri dongu
                100 => b[1] is >= 64 and <= 127,           // CGNAT 100.64/10
                169 => b[1] == 254,                        // link-local, bulut meta veri
                172 => b[1] is >= 16 and <= 31,            // ozel 172.16/12
                192 => (b[1] == 168)                       // ozel 192.168/16
                    || (b[1] == 0 && b[2] is 0 or 2),      // IETF 192.0.0/24, TEST-NET-1
                198 => b[1] is 18 or 19,                   // kiyaslama 198.18/15
                >= 224 => true,                            // cok noktaya yayin ve ayrilmis
                _ => false
            };
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.Equals(IPAddress.IPv6None) || address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            {
                return true;
            }

            var b = address.GetAddressBytes();
            return (b[0] & 0xFE) == 0xFC;                  // benzersiz yerel fc00::/7
        }

        return true;
    }
}
