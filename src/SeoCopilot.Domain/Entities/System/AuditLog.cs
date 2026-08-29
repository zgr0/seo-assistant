using System.Net;

namespace SeoCopilot.Domain.Entities.System;

public class AuditLog
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ActorId { get; set; }

    /// <summary>'site.created','crawl.started','content.generated' ...</summary>
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;

    /// <summary>jsonb — serbest govde.</summary>
    public string? Payload { get; set; }

    public IPAddress? Ip { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
