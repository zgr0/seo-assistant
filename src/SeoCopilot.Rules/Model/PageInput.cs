namespace SeoCopilot.Rules.Model;

/// <summary>
/// Kural motorunun tek girdi tipi. Application.ExtractedPage'den bagimsiz tutuldu ki
/// bu proje saf kalsin (Domain disinda referans yok).
/// </summary>
public sealed record PageInput
{
    public required string Url { get; init; }
    public int StatusCode { get; init; }
    public string? Title { get; init; }
    public string? MetaDescription { get; init; }
    public IReadOnlyList<string> H1 { get; init; } = [];
    public IReadOnlyList<string> InternalLinks { get; init; } = [];
    public int WordCount { get; init; }
    public bool HasCanonical { get; init; }
}
