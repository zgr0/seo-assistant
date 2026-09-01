using System.Net.Http.Json;
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
}

/// <summary>Anthropic Messages API — ham HTTP, harici SDK bagimliligi yok.</summary>
public sealed class AnthropicClient(HttpClient http, IOptions<AnthropicOptions> options)
    : IAnthropicClient
{
    private readonly AnthropicOptions _opt = options.Value;

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default) =>
        (await CompleteDetailedAsync(systemPrompt, userPrompt, ct)).Text;

    public async Task<CompletionResult> CompleteDetailedAsync(
        string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.ApiKey))
            throw new InvalidOperationException("Anthropic:ApiKey tanimli degil — icerik uretimi kapali");

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

        using var res = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<MessageResponse>(ct);
        return new CompletionResult(
            body?.Content.FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty,
            body?.Model ?? _opt.Model,
            body?.Usage?.InputTokens ?? 0,
            body?.Usage?.OutputTokens ?? 0);
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
