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

/// <summary>
/// Kural bazli bulgu ozeti — bir satir = bir kural, bir taramada onlarca sayfada tetiklenmis olsa da.
/// <see cref="IssueQuery.Status"/>, <see cref="IssueQuery.RuleCode"/> ve <see cref="IssueQuery.PageId"/>
/// bu sorguda uygulanmaz: acik ve yoksayilan adetler her zaman birlikte doner, boylece durum filtresi
/// istemcide adetleri tutarsizlastirmadan degistirilebilir.
/// </summary>
public sealed record IssueGroup(
    string RuleCode,
    string RuleTitle,
    Severity Severity,
    RuleCategory Category,
    int Weight,
    int OpenCount,
    int IgnoredCount);
