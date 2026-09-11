using Microsoft.Extensions.Logging;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Varyantlarin <c>imageBrief</c> alanindan gorsel uretir, depoya yazar ve varyanta baglar.
/// Gorsel zorunlu degildir: uretici kapaliysa ya da cagri basarisizsa is yine 'done' biter.
/// </summary>
public sealed class SocialImageService(
    IImageGenerator generator,
    ISocialImageComposer composer,
    IAssetStorage storage,
    IContentRepository content,
    ILogger<SocialImageService> logger)
{
    /// <summary>Tek iste uretilecek azami gorsel — maliyet freni.</summary>
    public const int MaxImagesPerJob = 5;

    /// <summary>Modelin metin/logo basmasini engelleyen kuyruk istemi.</summary>
    private const string PromptSuffix =
        "professional social media photography, natural lighting, high detail, " +
        "no text, no letters, no watermark, no logo";

    /// <summary>Platform gorsel orani; bilinmeyen platformda kare kullanilir.</summary>
    private static readonly Dictionary<string, string> AspectByPlatform =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["instagram"] = "1:1",
            ["facebook"] = "16:9",
            ["x"] = "16:9",
            ["linkedin"] = "16:9"
        };

    public async Task AttachAsync(ContentJob job, CancellationToken ct = default)
    {
        if (!generator.IsEnabled)
        {
            logger.LogDebug("Görsel üretimi kapalı — iş {JobId} görselsiz tamamlanıyor", job.Id);
            return;
        }

        var aspect = job.PlatformCode is { Length: > 0 } code
            && AspectByPlatform.TryGetValue(code, out var value) ? value : "1:1";

        foreach (var variant in job.Variants.OrderBy(v => v.VariantIndex).Take(MaxImagesPerJob))
        {
            // Model brief yazdiysa stil kuyrugunu biz ekleriz; yazmadiysa (sema disi yanit,
            // LLM kapali) tarama verisinden uretilen brief stilini zaten icerir.
            string prompt;
            if (variant.ImageBrief is { Length: > 0 } brief)
            {
                prompt = $"{brief}. {PromptSuffix}";
            }
            else
            {
                if (job.Page is null) continue;
                variant.ImageBrief = PageBriefBuilder.Build(job.Page);
                variant.ImageAlt ??= PageBriefBuilder.BuildAlt(job.Page);
                prompt = variant.ImageBrief;
            }

            try
            {
                var image = await generator.GenerateAsync(prompt, aspect, ct);
                if (image is null)
                {
                    logger.LogWarning("Görsel üretilemedi: iş {JobId}, varyant {Index}",
                        job.Id, variant.VariantIndex);
                    continue;
                }

                // Ham gorsel her zaman saklanir: yazi begenilmezse ya da baslik degisirse
                // yeniden uretim (ve yeni FLUX ucreti) gerekmeden yeniden basilabilir.
                var raw = await SaveAsync(
                    job, ContentAssetKind.Raw, image.Content, image.ContentType,
                    image.Width, image.Height, prompt, image.Model, sourceAssetId: null, ct);

                variant.ImageAssetId = raw.Id;

                // Ayni baytlar uzerine yazi — ikinci bir uretim cagrisi YOK.
                var caption = ImageCaptionBuilder.Build(variant, job.Page, job.BrandProfile);
                if (caption is not null && composer.Compose(image.Content, caption) is { } captioned)
                {
                    var withText = await SaveAsync(
                        job, ContentAssetKind.Captioned, captioned.Content, captioned.ContentType,
                        captioned.Width, captioned.Height, prompt, image.Model, raw.Id, ct);

                    // Varyant yazili surumu gosterir; ham surum indirme secenegi olarak kalir.
                    variant.ImageAssetId = withText.Id;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Gorsel kozmetiktir — metin uretimini bosa dusurme.
                logger.LogWarning(ex, "Görsel adımı başarısız: iş {JobId}, varyant {Index}",
                    job.Id, variant.VariantIndex);
            }
        }
    }

    /// <summary>Bayt icerigini depoya, meta veriyi <c>content_assets</c> satirina yazar.</summary>
    private async Task<ContentAsset> SaveAsync(
        ContentJob job, ContentAssetKind kind, byte[] bytes, string contentType,
        int width, int height, string prompt, string model, Guid? sourceAssetId,
        CancellationToken ct)
    {
        var asset = new ContentAsset
        {
            TenantId = job.TenantId,
            JobId = job.Id,
            Kind = kind,
            SourceAssetId = sourceAssetId,
            ContentType = contentType,
            Width = width,
            Height = height,
            Bytes = bytes.Length,
            Prompt = prompt,
            Model = model
        };
        asset.StorageKey = $"{job.TenantId}/{job.Id}/{asset.Id}{Extension(contentType)}";

        await storage.SaveAsync(asset.StorageKey, bytes, ct);
        await content.AddContentAssetAsync(asset, ct);
        return asset;
    }

    private static string Extension(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".jpg"
    };
}
