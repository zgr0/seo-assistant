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
    IContentQueue queue,
    IImageGenerator generator,
    SocialImageSettings imageSettings)
{
    /// <summary>Platform basina uretilecek azami gonderi (= secilecek sayfa) sayisi.</summary>
    public const int MaxPostCount = 5;

    public const int DefaultPostCount = 1;

    /// <summary>Tek istekte acilabilecek azami is — maliyet freni.</summary>
    public const int MaxJobsPerRequest = 12;

    /// <summary>Sayfa secimi icin son taramadan okunacak azami satir.</summary>
    private const int PageScanLimit = 200;

    public async Task<SocialKitResponse> CreateAsync(
        CreateSocialKitRequest request, Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var platformCodes = NormalizePlatforms(request.PlatformCodes);
        var templates = ImageTemplatePicker.Parse(request.ImageTemplates);
        var postCount = Math.Clamp(request.PostCount ?? DefaultPostCount, 1, MaxPostCount);
        var aiImages = request.AiImages is true;

        if (platformCodes.Count * postCount > MaxJobsPerRequest)
        {
            throw new InvalidOperationException(
                $"Platform x gönderi sayısı en fazla {MaxJobsPerRequest} olabilir; " +
                "daha az platform seçin ya da gönderi sayısını düşürün");
        }

        if (aiImages)
        {
            if (!generator.IsEnabled)
                throw new InvalidOperationException("Yapay zekâ görseli yapılandırılmamış (Cloudflare anahtarı yok)");

            // Afis ve alinti marka karti zemini ister — AI hic cagrilmazdi.
            var effective = templates.Count > 0 ? templates : imageSettings.Templates;
            if (ImageTemplatePicker.WantsCardOnly(effective))
                throw new InvalidOperationException("Fotoğrafsız şablonlarla yapay zekâ görseli kullanılamaz");
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

        // Onceki uretimler: az kullanilan sayfa once secilir, ayni sayfa tekrar gelirse aci kayar.
        var history = PostHistory.From(
            await content.ListSocialPostsAsync(tenantId, request.SiteId, PostHistory.MaxRecords, ct));
        var selected = PageSelector.SelectWithKinds(pages, postCount, page => history.UsageOf(page.Url));
        if (selected.Count == 0)
        {
            throw new InvalidOperationException(
                "Taramada gönderi üretmeye uygun sayfa bulunamadı — " +
                "sayfaların 200 dönmesi, dizine açık olması ve metin içermesi gerekir");
        }

        // Is sayisi sayfa seciminden sonra kesinlesir; sinir o zaman olculur.
        if (aiImages) await EnsureAiQuotaAsync(tenantId, platformCodes.Count * selected.Count, ct);

        // Her sayfada tekrarlanan sablon gorselleri (logo, katalog afisi) site butununden bulunur;
        // worker yalniz kendi sayfasini gordugu icin adaylar burada hesaplanip ise yazilir.
        var siteWideImages = PageImageCandidates.SiteWideImages(pages);

        var jobs = new List<ContentJob>(platformCodes.Count * selected.Count);
        for (var platformIndex = 0; platformIndex < platformCodes.Count; platformIndex++)
        {
            var code = platformCodes[platformIndex];
            for (var index = 0; index < selected.Count; index++)
            {
                var (page, kind) = selected[index];

                // Sayfa basina tek gonderi; cesitlilik sayfalardan gelir.
                // postIndex sablon acisini ve kalibini dondurur: paketteki sira, sayfanin
                // gecmis kullanimi ve platform sirasi eklenir — ayni sayfa tekrar secildiginde
                // ya da iki platforma birden yazildiginda ayni metin cikmaz. Worker yine de
                // gecmisle karsilastirir (bkz. ContentService). pageKind site butunune gore
                // verilmis turdur — worker sayfayi tek basina yeniden siniflamaz.
                var input = new JsonObject
                {
                    ["variantCount"] = 1,
                    ["postIndex"] = index + history.UsageOf(page.Url) + platformIndex,
                    ["pageUrl"] = page.Url,
                    ["pageKind"] = kind.ToString().ToLowerInvariant(),
                    ["imageCandidates"] = new JsonArray(
                        [.. PageImageCandidates.For(page, siteWideImages).Select(u => (JsonNode)u)]),
                    // Kullanicinin formda sectigi sablonlar; bossa ayarlardaki liste gecerli.
                    [ImageTemplatePicker.InputKey] = new JsonArray(
                        [.. templates.Select(t => (JsonNode)t.ToString())])
                };

                // Worker kaynak sirasini AI -> site -> kart yapar; yoksa ayarlardaki sira gecerli.
                if (aiImages) input[SocialImageSettings.InputKey] = SocialImageSettings.AiSource;

                jobs.Add(new ContentJob
                {
                    TenantId = tenantId,
                    SiteId = request.SiteId,
                    PageId = page.Id,
                    BrandProfileId = request.BrandProfileId,
                    Type = ContentJobType.SocialKit,
                    PlatformCode = code,
                    Input = input.ToJsonString(),
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
            [.. selected.Select(p => p.Page.Url)]);
    }

    /// <summary>Formdaki yapay zeka dugmesi: acik mi, gunluk sinir ve bugun kullanilan.</summary>
    public async Task<SocialImageSettingsDto> GetImageSettingsAsync(Guid tenantId, CancellationToken ct = default) =>
        new(generator.IsEnabled,
            imageSettings.MaxAiImagesPerDay,
            await content.CountAiImageJobsSinceAsync(tenantId, StartOfTodayUtc(), ct));

    /// <summary>
    /// Kiracinin bugunku yapay zeka hakki bu paketi karsilamiyorsa 409. Ayni anda gelen iki
    /// istek sinirin biraz ustune cikabilir — sinir maliyet freni, kesin kota degil.
    /// </summary>
    private async Task EnsureAiQuotaAsync(Guid tenantId, int requested, CancellationToken ct)
    {
        var limit = imageSettings.MaxAiImagesPerDay;
        var used = await content.CountAiImageJobsSinceAsync(tenantId, StartOfTodayUtc(), ct);
        var remaining = Math.Max(0, limit - used);

        if (requested <= remaining) return;

        throw new ConflictException(remaining == 0
            ? $"Bugünkü yapay zekâ görseli sınırı ({limit}) doldu — yarın tekrar deneyin ya da gönderileri yapay zekâsız üretin"
            : $"Bugün {remaining} yapay zekâ görseli hakkı kaldı, bu paket {requested} görsel istiyor — " +
              "gönderi ya da platform sayısını azaltın");
    }

    private static DateTimeOffset StartOfTodayUtc() => new(DateTime.UtcNow.Date, TimeSpan.Zero);

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
