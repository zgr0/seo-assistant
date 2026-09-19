using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;
using SkiaSharp;

namespace SeoCopilot.Infrastructure.Clients;

public sealed class CloudflareAiOptions
{
    public const string Section = "CloudflareAi";

    /// <summary>Cloudflare panelinde hesap kimligi (Workers AI sayfasinda da gorunur).</summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>"Workers AI" izni olan API token'i.</summary>
    public string ApiToken { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.cloudflare.com/client/v4";

    /// <summary>
    /// FLUX.2 [klein] 4B: genislik/yukseklik alir, 4 adimda biter, gorsel basina ~0.001-0.002 $.
    /// Gunluk 10.000 neuron ucretsiz (hesap geneli).
    /// </summary>
    public string Model { get; set; } = "@cf/black-forest-labs/flux-2-klein-4b";

    /// <summary>Tek istek (deneme) icin sure siniri.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Guvenlik filtresi (kod 3030) ciktiyi engellerse ek deneme sayisi. Engel rastgele — ayni
    /// istem bir denemede gecip digerinde takilabiliyor; her deneme yeni bir cikti uretir.
    /// </summary>
    public int MaxFlaggedRetries { get; set; } = 2;
}

/// <summary>
/// Cloudflare Workers AI metinden gorsel — senkron: tek multipart istek, yanitta base64 gorsel.
/// Guvenlik filtresi engelinde yeniden dener; hata, zaman asimi ya da gunluk kota dolmasinda null
/// doner, cagiran taraf siradaki kaynaga gecer.
/// </summary>
public sealed class CloudflareImageClient(
    HttpClient http, IOptions<CloudflareAiOptions> options, ILogger<CloudflareImageClient> logger)
    : IImageGenerator
{
    private readonly CloudflareAiOptions _opt = options.Value;

    /// <summary>Model daha uzun istemi reddeder.</summary>
    public const int MaxPromptLength = 2048;

    /// <summary>"Your output has been flagged" — zararsiz istemlerde de rastgele donebiliyor.</summary>
    public const int FlaggedErrorCode = 3030;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(_opt.AccountId) && !string.IsNullOrWhiteSpace(_opt.ApiToken);

    public async Task<GeneratedImage?> GenerateAsync(
        string prompt, string aspectRatio, CancellationToken ct = default)
    {
        if (!IsEnabled) return null;

        var (width, height) = SizeOf(aspectRatio);
        var clipped = Clip(prompt);

        for (var attempt = 0; ; attempt++)
        {
            var (image, flagged) = await SendAsync(clipped, width, height, ct);

            // Yalniz filtre engeli yeniden denenir — kota, yetki ve bicim hatalari tekrarla duzelmez.
            if (image is not null || !flagged) return image;

            if (attempt >= _opt.MaxFlaggedRetries)
            {
                logger.LogWarning("Cloudflare güvenlik filtresi {Attempts} denemenin hepsini engelledi", attempt + 1);
                return null;
            }

            logger.LogInformation("Cloudflare güvenlik filtresi görseli engelledi — yeniden deneniyor ({Retry}/{Max})",
                attempt + 1, _opt.MaxFlaggedRetries);
        }
    }

    /// <summary>Tek deneme. <c>Flagged</c>: cikti guvenlik filtresine takildi, yeniden denenebilir.</summary>
    private async Task<(GeneratedImage? Image, bool Flagged)> SendAsync(
        string prompt, int width, int height, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_opt.TimeoutSeconds));
        var token = timeout.Token;

        // Govde gonderimde tuketilir — her deneme kendi istegini kurar.
        using var form = new MultipartFormDataContent
        {
            { new StringContent(prompt), "prompt" },
            { new StringContent(width.ToString(CultureInfo.InvariantCulture)), "width" },
            { new StringContent(height.ToString(CultureInfo.InvariantCulture)), "height" }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, RunUrl()) { Content = form };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ApiToken);

        try
        {
            using var res = await http.SendAsync(req, token);
            var body = await res.Content.ReadAsStringAsync(token);

            if (IsFlagged(body)) return (null, true);

            if (!res.IsSuccessStatusCode)
            {
                // 429: gunluk ucretsiz kota ya da hiz siniri doldu.
                logger.LogWarning("Cloudflare görsel üretimi başarısız: {Status} - {Body}",
                    (int)res.StatusCode, Truncate(body));
                return (null, false);
            }

            if (ImageOf(body) is not { Length: > 0 } base64)
            {
                logger.LogWarning("Cloudflare yanıtında görsel yok: {Body}", Truncate(body));
                return (null, false);
            }

            var bytes = Convert.FromBase64String(base64);

            // Olcu ve bicim yanitin kendisinden okunur — model istenen olcuyu yuvarlayabilir.
            if (Describe(bytes) is not { } info)
            {
                logger.LogWarning("Cloudflare yanıtı çözülebilir bir görsel değil ({Bytes} bayt)", bytes.Length);
                return (null, false);
            }

            return (new GeneratedImage(bytes, info.ContentType, info.Width, info.Height, _opt.Model), false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Cloudflare görsel üretimi {Timeout} sn içinde tamamlanmadı", _opt.TimeoutSeconds);
            return (null, false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or FormatException)
        {
            logger.LogWarning(ex, "Cloudflare görsel üretimi başarısız");
            return (null, false);
        }
    }

    /// <summary>Hata listesinde <see cref="FlaggedErrorCode"/> var mi; JSON olmayan govde false.</summary>
    private static bool IsFlagged(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Array
                && errors.EnumerateArray().Any(e =>
                    e.ValueKind == JsonValueKind.Object
                    && e.TryGetProperty("code", out var code)
                    && code.ValueKind == JsonValueKind.Number
                    && code.TryGetInt32(out var value)
                    && value == FlaggedErrorCode);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Model kimligi (<c>@cf/...</c>) yola oldugu gibi eklenir — Cloudflare boyle bekler.</summary>
    private string RunUrl() =>
        $"{_opt.BaseUrl.TrimEnd('/')}/accounts/{Uri.EscapeDataString(_opt.AccountId)}/ai/run/{_opt.Model}";

    /// <summary>
    /// Platform oranina gore olcu; uzun kenar 1024 — neuron (ucret) karo sayisiyla artar.
    /// Kenarlar 16'nin katidir. Bilinmeyen oran kareye duser.
    /// </summary>
    private static (int Width, int Height) SizeOf(string aspectRatio) => aspectRatio switch
    {
        "16:9" => (1024, 576),
        "4:3" => (1024, 768),
        "3:4" => (768, 1024),
        "4:5" => (816, 1024),
        "9:16" => (576, 1024),
        _ => (1024, 1024)
    };

    /// <summary>REST zarfi <c>{"result":{"image":"..."},"success":true}</c>; zarfsiz yanit da kabul edilir.</summary>
    private static string? ImageOf(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False)
            return null;

        var holder = root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Object
            ? result
            : root;

        return holder.TryGetProperty("image", out var image) && image.ValueKind == JsonValueKind.String
            ? image.GetString()
            : null;
    }

    private static (string ContentType, int Width, int Height)? Describe(byte[] bytes)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        if (codec is null) return null;

        var contentType = codec.EncodedFormat switch
        {
            SKEncodedImageFormat.Jpeg => "image/jpeg",
            SKEncodedImageFormat.Png => "image/png",
            SKEncodedImageFormat.Webp => "image/webp",
            _ => null
        };

        return contentType is null ? null : (contentType, codec.Info.Width, codec.Info.Height);
    }

    private static string Clip(string prompt) =>
        prompt.Length <= MaxPromptLength ? prompt : prompt[..MaxPromptLength];

    private static string Truncate(string body) => body.Length <= 500 ? body : body[..500];
}
