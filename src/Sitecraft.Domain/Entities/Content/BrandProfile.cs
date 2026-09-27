using Sitecraft.Domain.Entities.Sites;
using Sitecraft.Domain.Entities.Tenancy;
using Sitecraft.Domain.Enums;

namespace Sitecraft.Domain.Entities.Content;

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

    /// <summary>
    /// Platform kodu -> hesap adi ('@' olmadan), ör. instagram -> ornekmarka. Link desteklemeyen
    /// platformda eylem cagrisi profile yonlendirirken kullanilir.
    /// </summary>
    public Dictionary<string, string> SocialHandles { get; set; } = [];

    /// <summary>Gorsel paneli ve marka karti rengi, '#RRGGBB'. Yoksa renk fotograftan/alan adindan turetilir.</summary>
    public string? PrimaryColor { get; set; }

    /// <summary>Vurgu rengi, '#RRGGBB'. Yoksa ana renkten turetilir.</summary>
    public string? AccentColor { get; set; }

    /// <summary>Logonun depo anahtari (bkz. IAssetStorage); PNG'ye normalize edilmis olarak saklanir.</summary>
    public string? LogoStorageKey { get; set; }

    /// <summary>
    /// Kapsamin varsayilani: <see cref="SiteId"/> doluysa o sitenin, bossa kiracinin. Kapsam basina
    /// en fazla bir varsayilan olur (veritabaninda tekil indeks).
    /// </summary>
    public bool IsDefault { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
