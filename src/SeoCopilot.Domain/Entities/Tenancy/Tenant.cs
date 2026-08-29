using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities.Tenancy;

/// <summary>Kiraci — faturalama, kota ve izolasyon siniri.</summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TenantPlan Plan { get; set; } = TenantPlan.Trial;

    /// <summary>Aylik taranabilir sayfa sayisi.</summary>
    public int PageQuota { get; set; }
    public int PagesUsed { get; set; }

    /// <summary>Aylik LLM token tavani.</summary>
    public long AiTokenQuota { get; set; }
    public long AiTokensUsed { get; set; }

    public DateTimeOffset? QuotaResetAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<User> Users { get; set; } = [];
}
