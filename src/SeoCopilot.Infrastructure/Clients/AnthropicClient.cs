using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Infrastructure.Clients;

public sealed class AnthropicOptions
{
    public const string Section = "Anthropic";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-5";
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// Workspace'e bagli olmayan organizasyon anahtarlari icin zorunlu; workspace'e bagli
    /// anahtarlarda bos birakilir.
    /// </summary>
    public string WorkspaceId { get; set; } = string.Empty;
}

/// <summary>Anthropic Messages API — ham HTTP, harici SDK bagimliligi yok.</summary>
public sealed class AnthropicClient(HttpClient http, IOptions<AnthropicOptions> options)
    : IAnthropicClient
{
    private readonly AnthropicOptions _opt = options.Value;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_opt.ApiKey);

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default) =>
        (await CompleteDetailedAsync(systemPrompt, userPrompt, ct)).Text;

    public async Task<CompletionResult> CompleteDetailedAsync(
        string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.ApiKey))
            throw new InvalidOperationException("Anthropic:ApiKey tanımlı değil — içerik üretimi kapalı");

        var payload = new
        {
            model = _opt.Model,
            max_tokens = _opt.MaxTokens,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userPrompt } }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Add("x-api-key", _opt.ApiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");

        // Workspace'e bagli olmayan (organizasyon seviyesi) anahtarlar bu basligi zorunlu kilar.
        if (!string.IsNullOrWhiteSpace(_opt.WorkspaceId))
            req.Headers.Add("anthropic-workspace-id", _opt.WorkspaceId);

        using var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            // Hata govdesi is kaydina yazilir; yoksa '400 Bad Request' teshis edilemez.
            var error = await res.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Anthropic {(int)res.StatusCode}: {ErrorMessage(error)}");
        }

        var body = await res.Content.ReadFromJsonAsync<MessageResponse>(ct);
        return new CompletionResult(
            body?.Content.FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty,
            body?.Model ?? _opt.Model,
            body?.Usage?.InputTokens ?? 0,
            body?.Usage?.OutputTokens ?? 0);
    }

    /// <summary>{"error":{"message":"..."}} govdesinden mesaji cikarir; JSON degilse ham metni kirpar.</summary>
    private static string ErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message)
                && message.GetString() is { Length: > 0 } text)
            {
                return text;
            }
        }
        catch (JsonException)
        {
            // JSON degil — asagidaki ham metin yedegine duser.
        }

        return body.Length > 500 ? body[..500] : body;
    }

    private sealed record MessageResponse(
        [property: JsonPropertyName("content")] IReadOnlyList<ContentBlock> Content,
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("usage")] Usage? Usage);

    private sealed record ContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);

    private sealed record Usage(
        [property: JsonPropertyName("input_tokens")] int InputTokens,
        [property: JsonPropertyName("output_tokens")] int OutputTokens);
}
