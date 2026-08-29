namespace SeoCopilot.Domain.Entities.Content;

/// <summary>Sosyal platform davranis kurallari — SEED verisi.</summary>
public class PlatformProfile
{
    /// <summary>'instagram'|'facebook'|'x'|'linkedin'</summary>
    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public int MaxChars { get; set; }
    public int RecommendedChars { get; set; }
    public int MaxHashtags { get; set; }

    /// <summary>X'te link erisimi dususu gibi — prompt'a yansir.</summary>
    public bool SupportsLinks { get; set; }

    /// <summary>Prompt'a enjekte edilen platform davranis notu.</summary>
    public string GuidanceTr { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ContentJob> ContentJobs { get; set; } = [];
}
