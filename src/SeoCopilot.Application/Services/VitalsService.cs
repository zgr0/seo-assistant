using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;

namespace SeoCopilot.Application.Services;

/// <summary>Sitenin Core Web Vitals gecmisi (PSI olcumleri crawl sirasinda yazilir).</summary>
public sealed class VitalsService(ISiteRepository repository)
{
    public const int DefaultTake = 30;
    public const int MaxTake = 200;

    public async Task<SiteVitalsDto> GetAsync(
        Guid siteId, Guid tenantId, string? url, int? take, CancellationToken ct = default)
    {
        _ = await repository.GetSiteForTenantAsync(siteId, tenantId, ct)
            ?? throw new NotFoundException($"Site {siteId} bulunamadi");

        var history = await repository.ListVitalsAsync(
            siteId, string.IsNullOrWhiteSpace(url) ? null : url.Trim(),
            Math.Clamp(take ?? DefaultTake, 1, MaxTake), ct);

        var items = history.Select(VitalDto.From).ToList();
        return new SiteVitalsDto(siteId, items.FirstOrDefault(), items);
    }
}
