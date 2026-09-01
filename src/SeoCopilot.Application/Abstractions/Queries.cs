using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Abstractions;

/// <summary>Sayfa listesi filtreleri — verilmeyen alan filtre uygulamaz.</summary>
public sealed record PageQuery(
    string? UrlContains = null,
    int? StatusCode = null,
    int? MinStatusCode = null,
    int? MaxStatusCode = null,
    int? Depth = null,
    bool? HasIssues = null);

/// <summary>Bulgu listesi filtreleri — verilmeyen alan filtre uygulamaz.</summary>
public sealed record IssueQuery(
    Severity? Severity = null,
    Severity? MinSeverity = null,
    RuleCategory? Category = null,
    string? RuleCode = null,
    IssueStatus? Status = null,
    Guid? PageId = null);
