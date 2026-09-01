using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Domain.Entities.Sites;

namespace SeoCopilot.Application.Services;

/// <summary>Site kaydi ve tarama ayarlari. Her islem kiraci sinirini uygular.</summary>
public sealed class SiteService(ISiteRepository repository)
{
    public async Task<SiteDto> CreateAsync(CreateSiteRequest request, Guid tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Site adi zorunlu");

        var baseUrl = UrlNormalizer.NormalizeSiteBaseUrl(request.BaseUrl)
            ?? throw new InvalidOperationException("Gecersiz site adresi — http/https bekleniyor");

        var site = new Site
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            BaseUrl = baseUrl
        };
        request.CrawlSettings?.ApplyTo(site.CrawlSettings);

        await repository.AddSiteAsync(site, ct);
        await repository.SaveChangesAsync(ct);

        return SiteDto.From(site);
    }

    public async Task<IReadOnlyList<SiteDto>> ListAsync(Guid tenantId, CancellationToken ct = default) =>
        [.. (await repository.ListSitesAsync(tenantId, ct)).Select(SiteDto.From)];

    public async Task<SiteDto> GetAsync(Guid siteId, Guid tenantId, CancellationToken ct = default) =>
        SiteDto.From(await RequireSiteAsync(siteId, tenantId, ct));

    /// <summary>Kismi guncelleme — verilmeyen alanlar korunur.</summary>
    public async Task<SiteDto> UpdateAsync(
        Guid siteId, Guid tenantId, UpdateSiteRequest request, CancellationToken ct = default)
    {
        var site = await RequireSiteAsync(siteId, tenantId, ct);

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new InvalidOperationException("Site adi bos olamaz");
            site.Name = request.Name.Trim();
        }

        if (request.BaseUrl is not null)
        {
            site.BaseUrl = UrlNormalizer.NormalizeSiteBaseUrl(request.BaseUrl)
                ?? throw new InvalidOperationException("Gecersiz site adresi — http/https bekleniyor");
        }

        if (request.IsActive is bool isActive) site.IsActive = isActive;
        if (request.ScheduleCron is not null)
            site.ScheduleCron = string.IsNullOrWhiteSpace(request.ScheduleCron) ? null : request.ScheduleCron.Trim();
        if (request.DefaultBrandProfileId is Guid brandId)
            site.DefaultBrandProfileId = brandId == Guid.Empty ? null : brandId;

        request.CrawlSettings?.ApplyTo(site.CrawlSettings);

        await repository.SaveChangesAsync(ct);
        return SiteDto.From(site);
    }

    /// <summary>Siteyi ve bagli tarama gecmisini siler.</summary>
    public async Task DeleteAsync(Guid siteId, Guid tenantId, CancellationToken ct = default)
    {
        var site = await RequireSiteAsync(siteId, tenantId, ct);
        await repository.RemoveSiteAsync(site, ct);
        await repository.SaveChangesAsync(ct);
    }

    public async Task<SiteDto> UpdateCrawlSettingsAsync(
        Guid siteId, Guid tenantId, CrawlSettingsDto request, CancellationToken ct = default)
    {
        var site = await RequireSiteAsync(siteId, tenantId, ct);
        request.ApplyTo(site.CrawlSettings);
        await repository.SaveChangesAsync(ct);
        return SiteDto.From(site);
    }

    private async Task<Site> RequireSiteAsync(Guid siteId, Guid tenantId, CancellationToken ct) =>
        await repository.GetSiteForTenantAsync(siteId, tenantId, ct)
            ?? throw new NotFoundException($"Site {siteId} bulunamadi");
}
