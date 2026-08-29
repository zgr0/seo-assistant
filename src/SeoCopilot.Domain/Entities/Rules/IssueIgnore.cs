using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Entities.Tenancy;

namespace SeoCopilot.Domain.Entities.Rules;

/// <summary>Bir kuralin belirli site/URL icin gormezden gelinmesi.</summary>
public class IssueIgnore
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SiteId { get; set; }
    public Site? Site { get; set; }

    public string RuleCode { get; set; } = string.Empty;

    /// <summary>null = tum site.</summary>
    public string? UrlPattern { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid CreatedBy { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
