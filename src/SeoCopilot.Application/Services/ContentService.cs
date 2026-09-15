using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Application.Services.Content;
using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services;

/// <summary>
/// Icerik uretimi: is kaydini yaratir, kuyruga atar; worker cagrisinda modeli calistirip
/// varyantlari yazar. Uretim senkron degildir — FE <c>GET /api/content/jobs/{id}</c> ile yoklar.
/// </summary>
public sealed class ContentService(
    IContentRepository content,
    ISiteRepository sites,
    IAnthropicClient llm,
    IContentQueue queue,
    SocialImageService images,
    IAssetStorage assets,
    ILogger<ContentService> logger)
{
    /// <summary>Tek istekte uretilebilecek azami is sayisi (toplu uretim).</summary>
    public const int MaxBatchSize = 50;

    /// <summary>Model yerine sablon kullanildiginda <c>content_jobs.model</c> degeri.</summary>
    public const string TemplateModel = "template";

    /// <summary>Platform kodu zorunlu olan is turleri.</summary>
    private static readonly ContentJobType[] PlatformRequired =
        [ContentJobType.SocialPost, ContentJobType.SocialBatch, ContentJobType.HashtagSet];

    public async Task<ContentJobDto> CreateAsync(
        GenerateContentRequest request, Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var job = await BuildJobAsync(
            request.Type, request.PlatformCode, request.PageId, request.BrandProfileId,
            request.Input, request.VariantCount, tenantId, userId, ct);

        await content.AddContentJobAsync(job, ct);
        await content.SaveChangesAsync(ct);

        queue.Enqueue(job.Id);
        return ContentJobDto.From(job);
    }

    /// <summary>Ayni tur ve platform icin sayfa basina bir is acar.</summary>
    public async Task<GenerateBatchResponse> CreateBatchAsync(
        GenerateBatchRequest request, Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var pageIds = request.PageIds?.Distinct().ToList() ?? [];
        if (pageIds.Count == 0)
            throw new InvalidOperationException("En az bir pageId gerekli");
        if (pageIds.Count > MaxBatchSize)
            throw new InvalidOperationException($"Tek seferde en fazla {MaxBatchSize} sayfa işlenebilir");

        var jobs = new List<ContentJob>(pageIds.Count);
        foreach (var pageId in pageIds)
        {
            jobs.Add(await BuildJobAsync(
                request.Type, request.PlatformCode, pageId, request.BrandProfileId,
                request.Input, request.VariantCount, tenantId, userId, ct));
        }

        foreach (var job in jobs)
            await content.AddContentJobAsync(job, ct);
        await content.SaveChangesAsync(ct);

        foreach (var job in jobs)
            queue.Enqueue(job.Id);

        return new GenerateBatchResponse([.. jobs.Select(j => j.Id)]);
    }

    /// <summary>Hangfire worker giris noktasi. Hata durumunda is 'failed' olarak isaretlenir.</summary>
    public async Task RunAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await content.GetContentJobAsync(jobId, ct)
            ?? throw new NotFoundException($"İçerik işi {jobId} bulunamadı");

        // Yeniden denemede tamamlanmis isi tekrar uretme.
        if (job.Status is ContentJobStatus.Done or ContentJobStatus.Running) return;

        job.Status = ContentJobStatus.Running;
        job.ErrorMessage = null;
        await content.SaveChangesAsync(ct);

        try
        {
            var platform = job.PlatformCode is null
                ? null
                : await content.GetPlatformProfileAsync(job.PlatformCode, ct);

            var withImage = job.Type == ContentJobType.SocialKit;
            CompletionResult? result = null;

            // Sitede daha once yazilan gonderiler — ayni metin ikinci kez kaydedilmez.
            var history = withImage && job.SiteId is Guid siteId
                ? PostHistory.From(
                    await content.ListSocialPostsAsync(job.TenantId, siteId, PostHistory.MaxRecords, ct),
                    exceptJobId: job.Id)
                : PostHistory.Empty;
            var previousPosts = withImage && job.Page is not null
                ? history.BodiesOf(job.Page.Url, MaxPreviousBodiesInPrompt)
                : null;

            // Sosyal pakette model zorunlu degil: anahtar yoksa ya da cagri duserse
            // gonderi tarama verisinden sablonla uretilir (bkz. PagePostBuilder).
            if (llm.IsEnabled || !withImage)
            {
                try
                {
                    result = await llm.CompleteDetailedAsync(
                        ContentPrompt.System(job.BrandProfile, platform, withImage),
                        ContentPrompt.User(job, job.Page, previousPosts),
                        ct);
                }
                catch (Exception ex) when (withImage && ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex,
                        "Model çağrısı başarısız — iş {JobId} şablonla üretiliyor", job.Id);
                }
            }

            if (result is not null)
            {
                foreach (var variant in ContentResponseParser.Parse(result.Text))
                {
                    // Model istemdeki eski gonderiyi aynen tekrarladiysa alinmaz; sablona dusulur.
                    if (withImage && history.Contains(variant.Body)) continue;

                    variant.JobId = job.Id;
                    job.Variants.Add(variant);
                }
            }

            if (job.Variants.Count == 0 && withImage && job.Page is not null)
            {
                var variant = BuildUniqueTemplate(job, platform, history);
                variant.JobId = job.Id;
                job.Variants.Add(variant);
            }

            // Gorsel adimi metinden sonra gelir; basarisiz olursa is yine 'done' biter.
            if (withImage) await images.AttachAsync(job, ct);

            job.Model = result?.Model ?? (withImage ? TemplateModel : null);
            job.TokensIn = result?.InputTokens ?? 0;
            job.TokensOut = result?.OutputTokens ?? 0;
            job.Status = ContentJobStatus.Done;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await content.SaveChangesAsync(ct);

            logger.LogInformation(
                "Icerik isi {JobId} tamamlandi: {Variants} varyant, {TokensIn}/{TokensOut} token",
                job.Id, job.Variants.Count, job.TokensIn, job.TokensOut);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Icerik isi {JobId} basarisiz", job.Id);
            job.Status = ContentJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;

            try
            {
                await content.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception saveEx)
            {
                // Hata kaydi da duserse (bozuk degisiklik izleyicisi) asil hatayi gizleme.
                logger.LogError(saveEx, "Icerik isi {JobId} 'failed' olarak isaretlenemedi", job.Id);
            }
        }
    }

    public async Task<ContentJobDto?> GetJobAsync(Guid jobId, Guid tenantId, CancellationToken ct = default)
    {
        var job = await content.GetContentJobForTenantAsync(jobId, tenantId, ct);
        return job is null ? null : ContentJobDto.From(job);
    }

    public async Task<PagedResult<ContentJobDto>> ListJobsAsync(
        Guid tenantId, string? type, string? platformCode, string? status,
        Guid? siteId, Guid? pageId, int page, int size, CancellationToken ct = default)
    {
        var (pageNumber, pageSize) = Paging.Normalize(page, size);
        var (items, total) = await content.ListContentJobsAsync(
            tenantId,
            EnumText.ParseOptional<ContentJobType>(type, "type"),
            string.IsNullOrWhiteSpace(platformCode) ? null : platformCode.Trim().ToLowerInvariant(),
            EnumText.ParseOptional<ContentJobStatus>(status, "status"),
            siteId,
            pageId,
            (pageNumber - 1) * pageSize,
            pageSize,
            ct);

        return new PagedResult<ContentJobDto>(
            [.. items.Select(ContentJobDto.From)], total, pageNumber, pageSize);
    }

    /// <summary>Daha once uretilen gorseller — galeri; yeniden eskiye, sayfalanmis.</summary>
    public async Task<PagedResult<ContentAssetDto>> ListAssetsAsync(
        Guid tenantId, Guid? siteId, int page, int size, CancellationToken ct = default)
    {
        var (pageNumber, pageSize) = Paging.Normalize(page, size);
        var (items, total) = await content.ListDisplayAssetsAsync(
            tenantId, siteId, (pageNumber - 1) * pageSize, pageSize, ct);

        return new PagedResult<ContentAssetDto>(
            [.. items.Select(ContentAssetDto.From)], total, pageNumber, pageSize);
    }

    /// <summary>Uretilen gorseli depodan okur; kiraci sinirini uygular.</summary>
    public async Task<(byte[] Content, string ContentType)> GetAssetAsync(
        Guid assetId, Guid tenantId, CancellationToken ct = default)
    {
        var asset = await content.GetAssetForTenantAsync(assetId, tenantId, ct)
            ?? throw new NotFoundException($"Görsel {assetId} bulunamadı");

        var bytes = await assets.ReadAsync(asset.StorageKey, ct)
            ?? throw new NotFoundException($"Görsel dosyası bulunamadı: {asset.StorageKey}");

        return (bytes, asset.ContentType);
    }

    /// <summary>Govde verilmezse favoriye ekler; <c>isFavorite:false</c> favoriden cikarir.</summary>
    public async Task<ContentVariantDto> SetFavoriteAsync(
        Guid variantId, Guid tenantId, bool? isFavorite, CancellationToken ct = default)
    {
        var variant = await content.GetVariantForTenantAsync(variantId, tenantId, ct)
            ?? throw new NotFoundException($"Varyant {variantId} bulunamadı");

        variant.IsFavorite = isFavorite ?? true;
        await content.SaveChangesAsync(ct);
        return ContentVariantDto.From(variant);
    }

    /// <summary>Secili islerin varyantlarini CSV'ye dokur (UTF-8 BOM — Excel icin).</summary>
    public async Task<byte[]> ExportCsvAsync(
        Guid tenantId, IReadOnlyCollection<Guid> jobIds, CancellationToken ct = default)
    {
        if (jobIds.Count == 0)
            throw new InvalidOperationException("En az bir jobId gerekli");

        var jobs = await content.GetContentJobsAsync(tenantId, jobIds, ct);

        var csv = new StringBuilder();
        csv.AppendLine("job_id,type,platform_code,page_url,status,created_at,variant_index,angle,body,hashtags,cta,char_count,is_favorite");

        foreach (var job in jobs)
        {
            if (job.Variants.Count == 0)
            {
                csv.AppendLine(Row(job, null));
                continue;
            }

            foreach (var variant in job.Variants.OrderBy(v => v.VariantIndex))
                csv.AppendLine(Row(job, variant));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();

        static string Row(ContentJob job, ContentVariant? v) => string.Join(',',
            Csv(job.Id.ToString()),
            Csv(job.Type.ToString()),
            Csv(job.PlatformCode),
            Csv(job.Page?.Url),
            Csv(job.Status.ToString()),
            Csv(job.CreatedAt.ToString("O")),
            Csv(v?.VariantIndex.ToString()),
            Csv(v?.Angle),
            Csv(v?.Body),
            Csv(v is null ? null : string.Join(' ', v.Hashtags)),
            Csv(v?.Cta),
            Csv(v?.CharCount.ToString()),
            Csv(v?.IsFavorite.ToString().ToLowerInvariant()));
    }

    /// <summary>RFC 4180: cift tirnak ikile, alani her zaman tirnak icine al.</summary>
    private static string Csv(string? value) =>
        $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

    private async Task<ContentJob> BuildJobAsync(
        string type, string? platformCode, Guid? pageId, Guid? brandProfileId,
        JsonElement? input, int? variantCount, Guid tenantId, Guid userId, CancellationToken ct)
    {
        var jobType = EnumText.ParseRequired<ContentJobType>(type, "type");

        string? platform = null;
        if (!string.IsNullOrWhiteSpace(platformCode))
        {
            var code = platformCode.Trim().ToLowerInvariant();
            _ = await content.GetPlatformProfileAsync(code, ct)
                ?? throw new NotFoundException($"Platform '{code}' bulunamadı");
            platform = code;
        }
        else if (PlatformRequired.Contains(jobType))
        {
            throw new InvalidOperationException($"'{type}' türü için platformCode zorunlu");
        }

        Guid? siteId = null;
        if (pageId is Guid id)
        {
            var page = await sites.GetPageForTenantAsync(id, tenantId, ct)
                ?? throw new NotFoundException($"Sayfa {id} bulunamadi");
            siteId = page.Crawl?.SiteId;
        }

        if (brandProfileId is Guid brandId)
        {
            _ = await content.GetBrandProfileAsync(brandId, tenantId, ct)
                ?? throw new NotFoundException($"Marka profili {brandId} bulunamadı");
        }

        return new ContentJob
        {
            TenantId = tenantId,
            SiteId = siteId,
            PageId = pageId,
            BrandProfileId = brandProfileId,
            Type = jobType,
            PlatformCode = platform,
            Input = BuildInput(input, variantCount),
            Status = ContentJobStatus.Queued,
            CreatedBy = userId
        };
    }

    /// <summary>Sablonun gecmiste olmayan bir metin bulmak icin deneyecegi azami kalip.</summary>
    public const int MaxTemplateAttempts = 36;

    /// <summary>Model istemine "bunlari tekrarlama" diye verilen eski gonderi sayisi.</summary>
    private const int MaxPreviousBodiesInPrompt = 5;

    /// <summary>
    /// Isin <c>postIndex</c>'inden baslayip sitede daha once yazilmamis ilk sablon metnini doner.
    /// Butun kaliplar tukenirse (cok kisa, tek cumlelik sayfa) ilk aday kullanilir ve loglanir.
    /// </summary>
    private ContentVariant BuildUniqueTemplate(ContentJob job, PlatformProfile? platform, PostHistory history)
    {
        var page = job.Page!;
        var kind = ContentPrompt.PageKindOf(ContentPrompt.ParseInput(job.Input), page);
        var start = VariantIndexOf(job);

        ContentVariant? first = null;
        for (var attempt = 0; attempt < MaxTemplateAttempts; attempt++)
        {
            var candidate = PagePostBuilder.Build(page, platform, job.BrandProfile, start + attempt, kind);
            if (!history.Contains(candidate.Body)) return candidate;
            first ??= candidate;
        }

        logger.LogWarning(
            "İş {JobId}: sayfa {Url} için yeni şablon metni kalmadı — önceki bir gönderi tekrarlanıyor",
            job.Id, page.Url);
        return first!;
    }

    /// <summary>
    /// Gonderiyi (isi), varyantlarini ve gorsellerini siler. Uretim suren is silinmez — worker
    /// yazmaya devam edip hata verirdi; takilip kalmis eski isler ise silinebilir.
    /// </summary>
    public async Task DeleteJobAsync(Guid jobId, Guid tenantId, CancellationToken ct = default)
    {
        var job = await content.GetContentJobForTenantAsync(jobId, tenantId, ct)
            ?? throw new NotFoundException($"İçerik işi {jobId} bulunamadı");

        if (job.Status is ContentJobStatus.Queued or ContentJobStatus.Running
            && job.CreatedAt > DateTimeOffset.UtcNow - StuckJobAge)
        {
            throw new ConflictException("Üretim sürüyor — bitince silebilirsiniz");
        }

        var keys = await content.ListAssetKeysForJobAsync(job.Id, ct);
        await content.DeleteContentJobAsync(job.Id, ct);

        // Kayit gitti; dosya silinemezse yalniz diskte artik kalir, istek basarisiz sayilmaz.
        foreach (var key in keys)
        {
            try
            {
                await assets.DeleteAsync(key, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Görsel dosyası silinemedi: {Key}", key);
            }
        }
    }

    /// <summary>Bu sureden eski 'queued/running' is takilmis sayilir ve silinebilir.</summary>
    private static readonly TimeSpan StuckJobAge = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Paketteki kacinci gonderi oldugu — sablon acisi (bilgilendirici/merak/satis) buna gore doner.
    /// <see cref="Social.SocialKitService"/> is acarken <c>postIndex</c> yazar.
    /// </summary>
    private static int VariantIndexOf(ContentJob job) =>
        ContentPrompt.PostIndexOf(ContentPrompt.ParseInput(job.Input));

    /// <summary>Serbest girdi govdesini normalize eder ve varyant sayisini icine yazar.</summary>
    private static string BuildInput(JsonElement? input, int? variantCount)
    {
        var node = new JsonObject();
        if (input is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
                node[property.Name] = JsonNode.Parse(property.Value.GetRawText());
        }

        if (variantCount is int count)
            node["variantCount"] = Math.Clamp(count, 1, ContentPrompt.MaxVariantCount);

        return node.ToJsonString();
    }
}
