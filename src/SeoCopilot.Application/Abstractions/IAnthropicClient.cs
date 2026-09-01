namespace SeoCopilot.Application.Abstractions;

/// <summary>Anthropic Messages API sarmalayicisi. Infrastructure katmani implemente eder.</summary>
public interface IAnthropicClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);

    /// <summary>Metinle birlikte model adi ve token sayimini doner — content_jobs maliyet alanlari icin.</summary>
    Task<CompletionResult> CompleteDetailedAsync(
        string systemPrompt, string userPrompt, CancellationToken ct = default);
}

public record CompletionResult(string Text, string Model, int InputTokens, int OutputTokens);
