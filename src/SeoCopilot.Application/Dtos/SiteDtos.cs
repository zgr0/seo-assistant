using SeoCopilot.Domain.Entities.Json;
using SeoCopilot.Domain.Entities.Sites;

namespace SeoCopilot.Application.Dtos;

public record CreateSiteRequest(string Name, string BaseUrl, CrawlSettingsDto? CrawlSettings = null);

/// <summary>PATCH /sites/{id} govdesi — kismi guncelleme. Verilmeyen alanlar korunur.</summary>
public record UpdateSiteRequest(
    string? Name = null,
    string? BaseUrl = null,
    bool? IsActive = null,
    string? ScheduleCron = null,
    Guid? DefaultBrandProfileId = null,
    CrawlSettingsDto? CrawlSettings = null);

/// <summary>Kismi guncellemeye izin verir — verilmeyen alanlar mevcut degeri korur.</summary>
public record CrawlSettingsDto(
    int? MaxPages = null,
    int? MaxDepth = null,
    int? DelayMs = null,
    bool? RenderJs = null,
    int? Concurrency = null,
    int? MaxAssetChecks = null,
    List<string>? IncludePatterns = null,
    List<string>? ExcludePatterns = null)
{
    public static CrawlSettingsDto From(CrawlSettings s) => new(
        s.MaxPages, s.MaxDepth, s.DelayMs, s.RenderJs, s.Concurrency, s.MaxAssetChecks,
        [.. s.IncludePatterns], [.. s.ExcludePatterns]);

    /// <summary>Verilen alanlari hedefe uygular.</summary>
    public void ApplyTo(CrawlSettings target)
    {
        if (MaxPages is int maxPages) target.MaxPages = Math.Clamp(maxPages, 1, 10_000);
        if (MaxDepth is int maxDepth) target.MaxDepth = Math.Clamp(maxDepth, 0, 20);
        if (DelayMs is int delay) target.DelayMs = Math.Clamp(delay, 0, 60_000);
        if (RenderJs is bool render) target.RenderJs = render;
        if (Concurrency is int concurrency) target.Concurrency = Math.Clamp(concurrency, 1, 16);
        if (MaxAssetChecks is int assetChecks) target.MaxAssetChecks = Math.Clamp(assetChecks, 0, 5_000);
        if (IncludePatterns is not null) target.IncludePatterns = [.. IncludePatterns];
        if (ExcludePatterns is not null) target.ExcludePatterns = [.. ExcludePatterns];
    }
}

public record SiteDto(
    Guid Id,
    string Name,
    string BaseUrl,
    bool IsActive,
    DateTimeOffset CreatedAt,
    CrawlSettingsDto CrawlSettings,
    string? ScheduleCron = null,
    Guid? DefaultBrandProfileId = null)
{
    public static SiteDto From(Site s) => new(
        s.Id,
        s.Name,
        s.BaseUrl,
        s.IsActive,
        s.CreatedAt,
        CrawlSettingsDto.From(s.CrawlSettings),
        s.ScheduleCron,
        s.DefaultBrandProfileId);
}
