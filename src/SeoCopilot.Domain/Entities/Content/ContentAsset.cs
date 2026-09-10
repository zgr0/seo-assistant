using SeoCopilot.Domain.Entities.Tenancy;

namespace SeoCopilot.Domain.Entities.Content;

/// <summary>
/// Uretilen sosyal medya gorseli. Bayt icerigi depoda (bkz. IAssetStorage), burada
/// yalniz meta veri ve depo anahtari tutulur.
/// </summary>
public class ContentAsset
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid JobId { get; set; }
    public ContentJob? Job { get; set; }

    /// <summary>Depo anahtari — kiraci/is/varlik kirilimli goreli yol.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = "image/jpeg";
    public int Width { get; set; }
    public int Height { get; set; }
    public int Bytes { get; set; }

    /// <summary>Gorseli ureten istem — yeniden uretim ve denetim icin.</summary>
    public string? Prompt { get; set; }

    public string? Model { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
