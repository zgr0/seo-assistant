using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Application.Services.Content;
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
    ILogger<ContentService> logger)
{
    /// <summary>Tek istekte uretilebilecek azami is sayisi (toplu uretim).</summary>
    public const int MaxBatchSize = 50;

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
            throw new InvalidOperationException($"Tek seferde en fazla {MaxBatchSize} sayfa islenebilir");

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
            ?? throw new NotFoundException($"Icerik isi {jobId} bulunamadi");

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

            var result = await llm.CompleteDetailedAsync(
                ContentPrompt.System(job.BrandProfile, platform),
                ContentPrompt.User(job, job.Page),
                ct);

            foreach (var variant in ContentResponseParser.Parse(result.Text))
            {
                variant.JobId = job.Id;
                job.Variants.Add(variant);
            }

            job.Model = result.Model;
            job.TokensIn = result.InputTokens;
            job.TokensOut = result.OutputTokens;
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
            await content.SaveChangesAsync(CancellationToken.None);
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

    /// <summary>Govde verilmezse favoriye ekler; <c>isFavorite:false</c> favoriden cikarir.</summary>
    public async Task<ContentVariantDto> SetFavoriteAsync(
        Guid variantId, Guid tenantId, bool? isFavorite, CancellationToken ct = default)
    {
        var variant = await content.GetVariantForTenantAsync(variantId, tenantId, ct)
            ?? throw new NotFoundException($"Varyant {variantId} bulunamadi");

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
                ?? throw new NotFoundException($"Platform '{code}' bulunamadi");
            platform = code;
        }
        else if (PlatformRequired.Contains(jobType))
        {
            throw new InvalidOperationException($"'{type}' turu icin platformCode zorunlu");
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
                ?? throw new NotFoundException($"Marka profili {brandId} bulunamadi");
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
