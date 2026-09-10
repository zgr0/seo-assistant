using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Infrastructure.Clients;

public sealed class FluxOptions
{
    public const string Section = "Flux";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.fluxapi.ai";

    /// <summary>flux-kontext-pro (standart) veya flux-kontext-max (karmasik sahne).</summary>
    public string Model { get; set; } = "flux-kontext-pro";

    /// <summary>Tek gorsel icin toplam sure siniri (gonderim + yoklama + indirme).</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Yoklama araligi.</summary>
    public int PollIntervalMs { get; set; } = 2000;
}

/// <summary>
/// fluxapi.ai Flux Kontext API — asenkron: gonderim <c>taskId</c> doner, <c>record-info</c>
/// ucu <c>successFlag</c> 1 olana kadar yoklanir, sonuc URL'inden gorsel indirilir.
/// Uretilen URL'ler 14 gun sonra silinir, o yuzden bayt olarak kendi depomuza yazariz.
/// </summary>
public sealed class FluxImageClient(
    HttpClient http, IOptions<FluxOptions> options, ILogger<FluxImageClient> logger)
    : IImageGenerator
{
    private readonly FluxOptions _opt = options.Value;

    /// <summary>successFlag: 0 uretiliyor, 1 basarili, 2 gorev acilamadi, 3 uretim basarisiz.</summary>
    private const int Generating = 0;
    private const int Success = 1;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_opt.ApiKey);

    public async Task<GeneratedImage?> GenerateAsync(
        string prompt, string aspectRatio, CancellationToken ct = default)
    {
        if (!IsEnabled) return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_opt.TimeoutSeconds));
        var token = timeout.Token;

        var ratio = Normalize(aspectRatio);

        var taskId = await SubmitAsync(prompt, ratio, token);
        if (taskId is null) return null;

        var imageUrl = await PollAsync(taskId, token);
        if (imageUrl is null) return null;

        var content = await http.GetByteArrayAsync(imageUrl, token);
        var (width, height) = Dimensions(ratio);
        return new GeneratedImage(content, "image/jpeg", width, height, _opt.Model);
    }

    private async Task<string?> SubmitAsync(string prompt, string aspectRatio, CancellationToken ct)
    {
        var payload = new
        {
            prompt,
            aspectRatio,
            model = _opt.Model,
            outputFormat = "jpeg",
            // Tarama verisinden uretilen brief'ler Turkce olabilir; API yalniz Ingilizce
            // istem kabul ettigi icin ceviriyi acik birakiyoruz (Ingilizce istemde etkisiz).
            enableTranslation = true
        };

        using var req = new HttpRequestMessage(
            HttpMethod.Post, $"{_opt.BaseUrl}/api/v1/flux/kontext/generate")
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Add("Authorization", $"Bearer {_opt.ApiKey}");

        using var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            logger.LogWarning("FLUX gönderimi başarısız: {Status} - {Body}",
                (int)res.StatusCode, await res.Content.ReadAsStringAsync(ct));
            return null;
        }

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<TaskData>>(ct);
        if (body?.Code != 200 || body.Data?.TaskId is not { Length: > 0 } taskId)
        {
            logger.LogWarning("FLUX görevi açılamadı: kod {Code} - {Message}", body?.Code, body?.Msg);
            return null;
        }

        return taskId;
    }

    /// <summary>Hazir olunca gorsel URL'ini doner; hata ya da zaman asiminda null.</summary>
    private async Task<string?> PollAsync(string taskId, CancellationToken ct)
    {
        var url = $"{_opt.BaseUrl}/api/v1/flux/kontext/record-info?taskId={Uri.EscapeDataString(taskId)}";

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(_opt.PollIntervalMs, ct);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("Authorization", $"Bearer {_opt.ApiKey}");

            using var res = await http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("FLUX yoklaması başarısız: {Status}", (int)res.StatusCode);
                return null;
            }

            var body = await res.Content.ReadFromJsonAsync<ApiResponse<RecordData>>(ct);
            if (body?.Code != 200)
            {
                logger.LogWarning("FLUX yoklama hatası: kod {Code} - {Message}", body?.Code, body?.Msg);
                return null;
            }

            switch (body.Data?.SuccessFlag)
            {
                case Success:
                    return body.Data.Response?.ResultImageUrl;

                case Generating or null:
                    continue;

                default:
                    logger.LogWarning("FLUX üretimi başarısız: successFlag {Flag} - {Error}",
                        body.Data.SuccessFlag, body.Data.ErrorMessage);
                    return null;
            }
        }

        logger.LogWarning("FLUX üretimi {Timeout} sn içinde tamamlanmadı", _opt.TimeoutSeconds);
        return null;
    }

    /// <summary>API yalniz belirli oranlari kabul eder; bilinmeyen deger kareye duser.</summary>
    private static string Normalize(string aspectRatio) => aspectRatio switch
    {
        "21:9" or "16:9" or "4:3" or "1:1" or "3:4" or "9:16" => aspectRatio,
        _ => "1:1"
    };

    /// <summary>Yalniz kayit icin yaklasik boyut — API piksel dondurmez.</summary>
    private static (int Width, int Height) Dimensions(string aspectRatio) => aspectRatio switch
    {
        "21:9" => (1536, 658),
        "16:9" => (1392, 784),
        "4:3" => (1184, 880),
        "3:4" => (880, 1184),
        "9:16" => (784, 1392),
        _ => (1024, 1024)
    };

    private sealed record ApiResponse<T>(
        [property: JsonPropertyName("code")] int Code,
        [property: JsonPropertyName("msg")] string? Msg,
        [property: JsonPropertyName("data")] T? Data);

    private sealed record TaskData(
        [property: JsonPropertyName("taskId")] string? TaskId);

    private sealed record RecordData(
        [property: JsonPropertyName("taskId")] string? TaskId,
        [property: JsonPropertyName("successFlag")] int? SuccessFlag,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage,
        [property: JsonPropertyName("response")] RecordResponse? Response);

    private sealed record RecordResponse(
        [property: JsonPropertyName("resultImageUrl")] string? ResultImageUrl);
}
