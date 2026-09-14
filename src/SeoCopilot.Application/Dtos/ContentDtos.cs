using System.Text.Json;
using SeoCopilot.Domain.Entities.Content;

namespace SeoCopilot.Application.Dtos;

// --- marka profili ---

public record CreateBrandProfileRequest(
    string Name,
    Guid? SiteId = null,
    string? Tone = null,
    string? AddressForm = null,
    string? EmojiUsage = null,
    List<string>? BannedPhrases = null,
    List<string>? DefaultHashtags = null,
    string? TargetAudience = null,
    string? ExtraContext = null,
    bool? IsDefault = null);

/// <summary>Kismi guncelleme — verilmeyen alanlar korunur.</summary>
public record UpdateBrandProfileRequest(
    string? Name = null,
    Guid? SiteId = null,
    string? Tone = null,
    string? AddressForm = null,
    string? EmojiUsage = null,
    List<string>? BannedPhrases = null,
    List<string>? DefaultHashtags = null,
    string? TargetAudience = null,
    string? ExtraContext = null,
    bool? IsDefault = null);

public record BrandProfileDto(
    Guid Id,
    Guid? SiteId,
    string Name,
    string Tone,
    string AddressForm,
    string EmojiUsage,
    IReadOnlyList<string> BannedPhrases,
    IReadOnlyList<string> DefaultHashtags,
    string? TargetAudience,
    string? ExtraContext,
    bool IsDefault,
    DateTimeOffset CreatedAt)
{
    public static BrandProfileDto From(BrandProfile p) => new(
        p.Id,
        p.SiteId,
        p.Name,
        p.Tone.ToString(),
        p.AddressForm.ToString(),
        p.EmojiUsage.ToString(),
        p.BannedPhrases,
        p.DefaultHashtags,
        p.TargetAudience,
        p.ExtraContext,
        p.IsDefault,
        p.CreatedAt);
}

public record PlatformProfileDto(
    string Code,
    string DisplayName,
    int MaxChars,
    int RecommendedChars,
    int MaxHashtags,
    bool SupportsLinks,
    string GuidanceTr,
    bool IsActive)
{
    public static PlatformProfileDto From(PlatformProfile p) => new(
        p.Code, p.DisplayName, p.MaxChars, p.RecommendedChars,
        p.MaxHashtags, p.SupportsLinks, p.GuidanceTr, p.IsActive);
}

// --- icerik uretimi ---

/// <summary>
/// <paramref name="Type"/> = title|meta_description|h1|product_description|blog_outline|
/// fix_advice|social_post|social_batch|hashtag_set. <paramref name="Input"/> serbest jsonb govde.
/// </summary>
public record GenerateContentRequest(
    string Type,
    string? PlatformCode = null,
    Guid? PageId = null,
    Guid? BrandProfileId = null,
    JsonElement? Input = null,
    int? VariantCount = null);

public record GenerateBatchRequest(
    string Type,
    string? PlatformCode,
    List<Guid> PageIds,
    Guid? BrandProfileId = null,
    JsonElement? Input = null,
    int? VariantCount = null);

public record GenerateBatchResponse(IReadOnlyList<Guid> JobIds);

public record ContentVariantDto(
    Guid Id,
    int VariantIndex,
    string? Angle,
    string Body,
    IReadOnlyList<string> Hashtags,
    string? Cta,
    int CharCount,
    bool IsFavorite,
    string? Description,
    string? ImageAlt,
    Guid? ImageAssetId,
    /// <summary>Yazisiz ham gorsel — yazili surumun kaynagi; indirme secenegi icin.</summary>
    Guid? RawImageAssetId)
{
    public static ContentVariantDto From(ContentVariant v) => new(
        v.Id, v.VariantIndex, v.Angle, v.Body, v.Hashtags, v.Cta, v.CharCount, v.IsFavorite,
        v.Description, v.ImageAlt, v.ImageAssetId, v.ImageAsset?.SourceAssetId);
}

public record ContentJobDto(
    Guid Id,
    string Type,
    string Status,
    string? PlatformCode,
    Guid? SiteId,
    Guid? PageId,
    string? PageUrl,
    Guid? BrandProfileId,
    string Input,
    string? Model,
    int TokensIn,
    int TokensOut,
    decimal CostUsd,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ContentVariantDto> Variants)
{
    public static ContentJobDto From(ContentJob j) => new(
        j.Id,
        j.Type.ToString(),
        j.Status.ToString(),
        j.PlatformCode,
        j.SiteId,
        j.PageId,
        j.Page?.Url,
        j.BrandProfileId,
        j.Input,
        j.Model,
        j.TokensIn,
        j.TokensOut,
        j.CostUsd,
        j.ErrorMessage,
        j.CreatedAt,
        j.CompletedAt,
        [.. j.Variants.OrderBy(v => v.VariantIndex).Select(ContentVariantDto.From)]);
}

/// <summary>Galeri satiri — yazili gorsel ya da yazili kopyasi olmayan ham gorsel.</summary>
/// <param name="RawAssetId">Yazili surumun kaynagi (yazisiz indirme icin); ham gorselde null.</param>
public record ContentAssetDto(
    Guid Id,
    string Kind,
    Guid? RawAssetId,
    int Width,
    int Height,
    int Bytes,
    Guid JobId,
    Guid? SiteId,
    string? PlatformCode,
    string? PageUrl,
    string? Alt,
    DateTimeOffset CreatedAt)
{
    public static ContentAssetDto From(ContentAsset a)
    {
        // Varyant yazili surumu, yazi basilamadiysa ham surumu isaret eder.
        var variant = a.Job?.Variants.FirstOrDefault(v => v.ImageAssetId == a.Id);

        return new ContentAssetDto(
            a.Id,
            a.Kind.ToString(),
            a.SourceAssetId,
            a.Width,
            a.Height,
            a.Bytes,
            a.JobId,
            a.Job?.SiteId,
            a.Job?.PlatformCode,
            a.Job?.Page?.Url,
            variant?.ImageAlt,
            a.CreatedAt);
    }
}

/// <summary>Bos govde = favoriye ekle.</summary>
public record FavoriteVariantRequest(bool? IsFavorite = null);
