namespace SeoCopilot.Application.Dtos;

/// <summary>
/// <paramref name="ApplyToSite"/> true ise kural site genelinde gormezden gelinir ve ayni
/// koda sahip acik bulgular da kapatilir; false ise yalniz bu bulgunun URL'i icin.
/// </summary>
public record IgnoreIssueRequest(string Reason, bool ApplyToSite = false);
