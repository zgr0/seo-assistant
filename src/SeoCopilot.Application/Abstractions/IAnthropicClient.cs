namespace SeoCopilot.Application.Abstractions;

/// <summary>Anthropic Messages API sarmalayicisi. Infrastructure katmani implemente eder.</summary>
public interface IAnthropicClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
