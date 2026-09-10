using System.Text.Json.Nodes;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Taranmis bir siteden ornek sosyal medya gonderileri uretir: sayfa secer, her
/// platform x sayfa icin bir <c>social_kit</c> isi acar ve kuyruga atar.
/// Uretim <see cref="ContentService"/> worker'inda yurur.
/// </summary>
public sealed class SocialKitService(
    IContentRepository content,
    ISiteRepository sites,
    IContentQueue queue)
{
    /// <summary>Platform basina uretilecek azami gonderi (= secilecek sayfa) sayisi.</summary>
    public const int MaxPostCount = 5;

    public const int DefaultPostCount = 3;

    /// <summary>Tek istekte acilabilecek azami is — maliyet freni.</summary>
    public const int MaxJobsPerRequest = 12;

    /// <summary>Sayfa secimi icin son taramadan okunacak azami satir.</summary>
    private const int PageScanLimit = 200;

    public async Task<SocialKitResponse> CreateAsync(
        CreateSocialKitRequest request, Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var platformCodes = NormalizePlatforms(request.PlatformCodes);
        var postCount = Math.Clamp(request.PostCount ?? DefaultPostCount, 1, MaxPostCount);

        if (platformCodes.Count * postCount > MaxJobsPerRequest)
        {
            throw new InvalidOperationException(
                $"Platform x gönderi sayısı en fazla {MaxJobsPerRequest} olabilir; " +
                "daha az platform seçin ya da gönderi sayısını düşürün");
        }

        _ = await sites.GetSiteForTenantAsync(request.SiteId, tenantId, ct)
            ?? throw new NotFoundException($"Site {request.SiteId} bulunamadı");

        // Ucuz dogrulamalar once: tarama durumundan bagimsiz olarak istek gecersizse hemen doner.
        foreach (var code in platformCodes)
        {
            _ = await content.GetPlatformProfileAsync(code, ct)
                ?? throw new NotFoundException($"Platform '{code}' bulunamadı");
        }

        if (request.BrandProfileId is Guid brandId)
        {
            _ = await content.GetBrandProfileAsync(brandId, tenantId, ct)
                ?? throw new NotFoundException($"Marka profili {brandId} bulunamadı");
        }

        var crawl = await sites.GetLatestCrawlAsync(request.SiteId, ct)
            ?? throw new InvalidOperationException("Bu sitede tarama yok — önce taramayı çalıştırın");

        // Partial tarama da kullanilir: sayfalarin bir kismi yazilmis olur.
        if (crawl.Status is not (CrawlStatus.Completed or CrawlStatus.Partial))
            throw new InvalidOperationException("Son tarama tamamlanmadı — bitmesini bekleyin");

        var pages = await sites.GetPagesAsync(crawl.Id, 0, PageScanLimit, ct);
        var selected = PageSelector.Select(pages, postCount);
        if (selected.Count == 0)
        {
            throw new InvalidOperationException(
                "Taramada gönderi üretmeye uygun sayfa bulunamadı — " +
                "sayfaların 200 dönmesi, dizine açık olması ve metin içermesi gerekir");
        }

        var jobs = new List<ContentJob>(platformCodes.Count * selected.Count);
        foreach (var code in platformCodes)
        {
            for (var index = 0; index < selected.Count; index++)
            {
                var page = selected[index];
                jobs.Add(new ContentJob
                {
                    TenantId = tenantId,
                    SiteId = request.SiteId,
                    PageId = page.Id,
                    BrandProfileId = request.BrandProfileId,
                    Type = ContentJobType.SocialKit,
                    PlatformCode = code,
                    // Sayfa basina tek gonderi; cesitlilik sayfalardan gelir.
                    // postIndex sablon uretiminde acinin donmesini saglar.
                    Input = new JsonObject { ["variantCount"] = 1, ["postIndex"] = index }.ToJsonString(),
                    Status = ContentJobStatus.Queued,
                    CreatedBy = userId
                });
            }
        }

        foreach (var job in jobs)
            await content.AddContentJobAsync(job, ct);
        await content.SaveChangesAsync(ct);

        foreach (var job in jobs)
            queue.Enqueue(job.Id);

        return new SocialKitResponse(
            request.SiteId,
            crawl.Id,
            [.. jobs.Select(j => j.Id)],
            [.. selected.Select(p => p.Url)]);
    }

    private static List<string> NormalizePlatforms(List<string>? codes)
    {
        var normalized = codes?
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToLowerInvariant())
            .Distinct()
            .ToList() ?? [];

        if (normalized.Count == 0)
            throw new InvalidOperationException("En az bir platform seçin");

        return normalized;
    }
}
