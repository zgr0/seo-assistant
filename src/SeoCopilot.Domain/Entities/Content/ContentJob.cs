using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Entities.Tenancy;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities.Content;

public class ContentJob
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid? SiteId { get; set; }
    public Site? Site { get; set; }

    public Guid? PageId { get; set; }
    public Page? Page { get; set; }

    public Guid? BrandProfileId { get; set; }
    public BrandProfile? BrandProfile { get; set; }

    public ContentJobType Type { get; set; }

    /// <summary>Sosyal uretimlerde dolu.</summary>
    public string? PlatformCode { get; set; }
    public PlatformProfile? Platform { get; set; }

    /// <summary>{currentTitle,keywords[],campaignAngle,maxLength,...} (jsonb)</summary>
    public string Input { get; set; } = "{}";

    public ContentJobStatus Status { get; set; } = ContentJobStatus.Queued;
    public string? ErrorMessage { get; set; }

    public string? Model { get; set; }
    public int TokensIn { get; set; }
    public int TokensOut { get; set; }
    public decimal CostUsd { get; set; }

    public Guid CreatedBy { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<ContentVariant> Variants { get; set; } = [];
}
