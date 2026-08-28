using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

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
    : Application.Abstractions.IAnthropicClient
{
    private readonly AnthropicOptions _opt = options.Value;

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
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
        return body?.Content.FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty;
    }

    private sealed record MessageResponse(
        [property: JsonPropertyName("content")] IReadOnlyList<ContentBlock> Content);

    private sealed record ContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}
