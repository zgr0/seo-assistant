using System.Security.Cryptography;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services;

/// <summary>Site kaydi, dogrulama ve tarama ayarlari. Her islem kiraci sinirini uygular.</summary>
public sealed class SiteService(ISiteRepository repository, ISiteVerifier verifier)
{
    /// <summary>Dogrulama icin siteye eklenmesi gereken meta etiketin adi.</summary>
    public const string VerificationMetaName = "seocopilot-verification";

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
            BaseUrl = baseUrl,
            VerificationMethod = VerificationMethod.MetaTag,
            VerificationToken = RandomNumberGenerator.GetHexString(32, lowercase: true)
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

    public async Task<VerifySiteResponse> VerifyAsync(Guid siteId, Guid tenantId, CancellationToken ct = default)
    {
        var site = await RequireSiteAsync(siteId, tenantId, ct);
        var token = site.VerificationToken
            ?? throw new InvalidOperationException("Site icin dogrulama anahtari uretilmemis");
        var metaTag = MetaTagFor(token);

        if (site.VerifiedAt is not null)
            return new VerifySiteResponse(true, site.VerifiedAt, metaTag);

        if (await verifier.VerifyMetaTagAsync(site.BaseUrl, token, ct))
        {
            site.VerifiedAt = DateTimeOffset.UtcNow;
            await repository.SaveChangesAsync(ct);
        }

        return new VerifySiteResponse(site.VerifiedAt is not null, site.VerifiedAt, metaTag);
    }

    public async Task<SiteDto> UpdateCrawlSettingsAsync(
        Guid siteId, Guid tenantId, CrawlSettingsDto request, CancellationToken ct = default)
    {
        var site = await RequireSiteAsync(siteId, tenantId, ct);
        request.ApplyTo(site.CrawlSettings);
        await repository.SaveChangesAsync(ct);
        return SiteDto.From(site);
    }

    public static string MetaTagFor(string token) =>
        $"<meta name=\"{VerificationMetaName}\" content=\"{token}\" />";

    private async Task<Site> RequireSiteAsync(Guid siteId, Guid tenantId, CancellationToken ct) =>
        await repository.GetSiteForTenantAsync(siteId, tenantId, ct)
            ?? throw new NotFoundException($"Site {siteId} bulunamadi");
}
