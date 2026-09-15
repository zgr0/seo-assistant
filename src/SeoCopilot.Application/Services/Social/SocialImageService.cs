using System.Text.Json;
using Microsoft.Extensions.Logging;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services.Content;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Gonderi gorselini ayarlardaki kaynak sirasiyla bulur (<see cref="SocialImageSettings"/>):
/// sitenin kendi fotografi, yapay zeka uretimi ya da marka karti. Ilk basarili kaynak
/// kullanilir, ustune yazi basilir, iki surum de saklanir.
/// Gorsel zorunlu degildir: hicbir kaynak sonuc vermezse is yine 'done' biter.
/// </summary>
public sealed class SocialImageService(
    SocialImageSettings settings,
    IImageGenerator generator,
    ISiteImageFetcher fetcher,
    IImageCanvas canvas,
    ISocialImageComposer composer,
    IAssetStorage storage,
    IContentRepository content,
    ILogger<SocialImageService> logger)
{
    /// <summary>Tek iste uretilecek azami gorsel — maliyet freni.</summary>
    public const int MaxImagesPerJob = 5;

    /// <summary><c>content_assets.model</c> — gorselin nereden geldigi.</summary>
    public const string SiteImageModel = "site-image";
    public const string CardModel = "brand-card";

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
        var aspect = job.PlatformCode is { Length: > 0 } code
            && AspectByPlatform.TryGetValue(code, out var value) ? value : "1:1";

        // Ayni isin varyantlari ayni sayfadan gelir; bir fotograf iki kez kullanilmasin.
        var candidates = new Queue<string>(ImageCandidatesOf(job));
        var seed = ColorSeedOf(job);
        var input = ContentPrompt.ParseInput(job.Input);
        var postIndex = ContentPrompt.PostIndexOf(input);

        // Formda secilen sablonlar ayarlardaki listenin onune gecer.
        var chosen = ImageTemplatePicker.FromInput(input);
        IReadOnlyCollection<ImageTemplate> templates = chosen.Count > 0 ? chosen : settings.Templates;
        var cardOnly = ImageTemplatePicker.WantsCardOnly(templates);

        foreach (var variant in job.Variants.OrderBy(v => v.VariantIndex).Take(MaxImagesPerJob))
        {
            try
            {
                var (image, prompt) = await ResolveAsync(job, variant, aspect, candidates, seed, cardOnly, ct);
                if (image is null)
                {
                    logger.LogWarning("Hiçbir kaynak görsel vermedi: iş {JobId}, varyant {Index}",
                        job.Id, variant.VariantIndex);
                    continue;
                }

                // Ham gorsel her zaman saklanir: yazi begenilmezse ya da baslik degisirse
                // kaynaga yeniden gidilmeden (ve AI ucreti dogmadan) yeniden basilabilir.
                var raw = await SaveAsync(
                    job, ContentAssetKind.Raw, image.Content, image.ContentType,
                    image.Width, image.Height, prompt, image.Model, sourceAssetId: null, ct);

                variant.ImageAssetId = raw.Id;

                // Ayni baytlar uzerine tasarim — kaynaga ikinci bir cagri YOK. Sablon gonderi
                // sirasiyla doner; marka kartinda fotografsiz sablonlar kullanilir.
                var caption = ImageCaptionBuilder.Build(variant, job.Page, job.BrandProfile);
                if (caption is not null)
                {
                    caption = caption with
                    {
                        Template = ImageTemplatePicker.Pick(
                            templates,
                            photo: image.Model != CardModel,
                            hasSubline: caption.Subline is not null,
                            index: postIndex + variant.VariantIndex),
                        ColorSeed = seed
                    };
                }

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

    /// <summary>Kaynaklari sirayla dener; ilk sonucu ve kaydedilecek istemi/aciklamayi doner.</summary>
    /// <param name="cardOnly">Yalniz fotografsiz sablon secildi — fotograf indirilmez, AI cagrilmaz.</param>
    private async Task<(GeneratedImage? Image, string Prompt)> ResolveAsync(
        ContentJob job, ContentVariant variant, string aspect, Queue<string> candidates,
        string seed, bool cardOnly, CancellationToken ct)
    {
        foreach (var source in settings.Sources)
        {
            var name = source.Trim().ToLowerInvariant();
            if (cardOnly && name is SocialImageSettings.SiteSource or SocialImageSettings.AiSource) continue;

            switch (name)
            {
                case SocialImageSettings.SiteSource:
                    while (candidates.TryDequeue(out var url))
                    {
                        var bytes = await fetcher.FetchAsync(url, ct);
                        // Kucuk ya da logo gibi uzun/ince gorseller Fit'te elenir; siradakine gec.
                        if (bytes is not null && canvas.Fit(bytes, aspect) is { } fitted)
                            return (fitted with { Model = SiteImageModel }, url);
                    }
                    break;

                case SocialImageSettings.AiSource:
                    if (!generator.IsEnabled) break;
                    var prompt = AiPrompt(job, variant);
                    if (prompt is null) break;
                    if (await generator.GenerateAsync(prompt, aspect, ct) is { } generated)
                        return (generated, prompt);
                    break;

                case SocialImageSettings.CardSource:
                    return (canvas.Card(aspect, seed) with { Model = CardModel }, $"marka kartı: {seed}");

                default:
                    logger.LogWarning("Bilinmeyen görsel kaynağı atlandı: {Source}", source);
                    break;
            }
        }

        return (null, string.Empty);
    }

    /// <summary>
    /// Model brief yazdiysa stil kuyrugunu biz ekleriz; yazmadiysa (sema disi yanit, LLM
    /// kapali) tarama verisinden uretilen brief stilini zaten icerir.
    /// </summary>
    private static string? AiPrompt(ContentJob job, ContentVariant variant)
    {
        if (variant.ImageBrief is { Length: > 0 } brief) return $"{brief}. {PromptSuffix}";
        if (job.Page is null) return null;

        variant.ImageBrief = PageBriefBuilder.Build(job.Page);
        variant.ImageAlt ??= PageBriefBuilder.BuildAlt(job.Page);
        return variant.ImageBrief;
    }

    /// <summary>Sitenin alan adi — kart ve tasarim renkleri bir sitede hep ayni kalsin.</summary>
    private static string ColorSeedOf(ContentJob job) =>
        Uri.TryCreate(job.Page?.Url, UriKind.Absolute, out var uri) ? uri.Host : job.TenantId.ToString();

    /// <summary><see cref="SocialKitService"/> is acarken yazdigi site gorseli adaylari.</summary>
    private static IEnumerable<string> ImageCandidatesOf(ContentJob job)
    {
        if (ContentPrompt.ParseInput(job.Input) is not JsonElement input
            || !input.TryGetProperty("imageCandidates", out var list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return list.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .Where(PageImageCandidates.LooksLikePhoto)
            .ToList();
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
