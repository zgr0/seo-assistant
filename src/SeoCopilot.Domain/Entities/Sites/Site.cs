using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Json;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities.Sites;

public class Site
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Normalize edilmis, sonda / olmayan kok URL.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public VerificationMethod VerificationMethod { get; set; } = VerificationMethod.MetaTag;
    public string? VerificationToken { get; set; }

    /// <summary>null ise tarama baslatilamaz.</summary>
    public DateTimeOffset? VerifiedAt { get; set; }

    public CrawlSettings CrawlSettings { get; set; } = new();

    /// <summary>null | cron ifadesi (orn. '0 3 * * 1').</summary>
    public string? ScheduleCron { get; set; }

    public Guid? DefaultBrandProfileId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Crawl> Crawls { get; set; } = [];
}
