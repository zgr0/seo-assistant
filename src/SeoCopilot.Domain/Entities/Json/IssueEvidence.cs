namespace SeoCopilot.Domain.Entities.Json;

/// <summary>issues.evidence (jsonb) govdesi — kuraldan kurala degisen kanit alanlari.</summary>
public sealed class IssueEvidence
{
    public string? Found { get; set; }
    public string? Expected { get; set; }
    public int? Length { get; set; }
    public List<string> SampleUrls { get; set; } = [];
}
