using Microsoft.Extensions.Logging;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Domain.Entities.Content;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Varyantlarin <c>imageBrief</c> alanindan gorsel uretir, depoya yazar ve varyanta baglar.
/// Gorsel zorunlu degildir: uretici kapaliysa ya da cagri basarisizsa is yine 'done' biter.
/// </summary>
public sealed class SocialImageService(
    IImageGenerator generator,
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

                var asset = new ContentAsset
                {
                    TenantId = job.TenantId,
                    JobId = job.Id,
                    ContentType = image.ContentType,
                    Width = image.Width,
                    Height = image.Height,
                    Bytes = image.Content.Length,
                    Prompt = prompt,
                    Model = image.Model
                };
                asset.StorageKey = $"{job.TenantId}/{job.Id}/{asset.Id}{Extension(image.ContentType)}";

                await storage.SaveAsync(asset.StorageKey, image.Content, ct);
                await content.AddContentAssetAsync(asset, ct);
                variant.ImageAssetId = asset.Id;
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

    private static string Extension(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".jpg"
    };
}
