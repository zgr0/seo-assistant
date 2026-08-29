namespace SeoCopilot.Domain.Entities.Content;

/// <summary>Bir content_job'in tek varyanti (v1'de jsonb idi, artik ayri satir).</summary>
public class ContentVariant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid JobId { get; set; }
    public ContentJob? Job { get; set; }

    public int VariantIndex { get; set; }

    /// <summary>'bilgilendirici'|'merak_uyandiran'|'satis_odakli'</summary>
    public string? Angle { get; set; }

    public string Body { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = [];
    public string? Cta { get; set; }
    public int CharCount { get; set; }
    public bool IsFavorite { get; set; }
}
