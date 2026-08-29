using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Entities.Tenancy;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities.Content;

/// <summary>Marka sesi profili — icerik uretiminde prompt'a enjekte edilir.</summary>
public class BrandProfile
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid? SiteId { get; set; }
    public Site? Site { get; set; }

    public string Name { get; set; } = string.Empty;
    public BrandTone Tone { get; set; } = BrandTone.Kurumsal;
    public AddressForm AddressForm { get; set; } = AddressForm.Siz;
    public EmojiUsage EmojiUsage { get; set; } = EmojiUsage.None;

    public List<string> BannedPhrases { get; set; } = [];
    public List<string> DefaultHashtags { get; set; } = [];

    public string? TargetAudience { get; set; }
    public string? ExtraContext { get; set; }

    public bool IsDefault { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
