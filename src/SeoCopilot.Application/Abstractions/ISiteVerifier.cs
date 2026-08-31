namespace SeoCopilot.Application.Abstractions;

/// <summary>Site sahipligini dogrular — kok sayfada beklenen meta etiketini arar.</summary>
public interface ISiteVerifier
{
    Task<bool> VerifyMetaTagAsync(string baseUrl, string token, CancellationToken ct = default);
}
