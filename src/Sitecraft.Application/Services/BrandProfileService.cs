using Microsoft.Extensions.Logging;
using Sitecraft.Application.Abstractions;
using Sitecraft.Application.Common;
using Sitecraft.Application.Dtos;
using Sitecraft.Application.Services.Content;
using Sitecraft.Domain.Entities.Content;
using Sitecraft.Domain.Enums;

namespace Sitecraft.Application.Services;

/// <summary>Marka sesi profilleri, logolari ve platform seed listesi.</summary>
public sealed class BrandProfileService(
    IContentRepository content,
    ISiteRepository sites,
    IAssetStorage storage,
    IImageCanvas canvas,
    ILogger<BrandProfileService> logger)
{
    /// <summary>Yuklenebilecek azami logo dosyasi.</summary>
    public const int MaxLogoBytes = 2 * 1024 * 1024;

    /// <summary>Logonun saklanan uzun kenari — gorselde birkac yuz pikselden fazla yer kaplamaz.</summary>
    public const int LogoMaxSide = 512;

    public async Task<IReadOnlyList<BrandProfileDto>> ListAsync(
        Guid tenantId, Guid? siteId, CancellationToken ct = default) =>
        [.. (await content.ListBrandProfilesAsync(tenantId, siteId, ct)).Select(BrandProfileDto.From)];

    public async Task<BrandProfileDto> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        BrandProfileDto.From(await RequireAsync(id, tenantId, ct));

    public async Task<BrandProfileDto> CreateAsync(
        CreateBrandProfileRequest request, Guid tenantId, CancellationToken ct = default)
    {
        await RequireSiteOrNullAsync(request.SiteId, tenantId, ct);

        var profile = new BrandProfile
        {
            TenantId = tenantId,
            SiteId = request.SiteId == Guid.Empty ? null : request.SiteId,
            Name = BrandProfileRules.Name(request.Name),
            Tone = EnumText.ParseOptional<BrandTone>(request.Tone, "tone") ?? BrandTone.Kurumsal,
            AddressForm = EnumText.ParseOptional<AddressForm>(request.AddressForm, "addressForm") ?? AddressForm.Siz,
            EmojiUsage = EnumText.ParseOptional<EmojiUsage>(request.EmojiUsage, "emojiUsage") ?? EmojiUsage.None,
            BannedPhrases = BrandProfileRules.BannedPhrases(request.BannedPhrases),
            DefaultHashtags = BrandProfileRules.Hashtags(request.DefaultHashtags),
            TargetAudience = TargetAudience(request.TargetAudience),
            ExtraContext = ExtraContext(request.ExtraContext),
            SocialHandles = await HandlesAsync(request.SocialHandles, ct),
            PrimaryColor = BrandProfileRules.Color(request.PrimaryColor, "ana renk"),
            AccentColor = BrandProfileRules.Color(request.AccentColor, "vurgu rengi"),
            IsDefault = request.IsDefault ?? false
        };

        // Eski varsayilan once duser — kapsam basina tek varsayilan tekil indeksle korunuyor.
        if (profile.IsDefault)
            await content.ClearDefaultBrandProfilesAsync(tenantId, profile.SiteId, profile.Id, ct);

        await content.AddBrandProfileAsync(profile, ct);
        await content.SaveChangesAsync(ct);

        return BrandProfileDto.From(profile);
    }

    public async Task<BrandProfileDto> UpdateAsync(
        Guid id, Guid tenantId, UpdateBrandProfileRequest request, CancellationToken ct = default)
    {
        var profile = await RequireAsync(id, tenantId, ct);

        if (request.Name is not null) profile.Name = BrandProfileRules.Name(request.Name);

        if (request.SiteId is Guid siteId)
        {
            await RequireSiteOrNullAsync(siteId, tenantId, ct);
            profile.SiteId = siteId == Guid.Empty ? null : siteId;
        }

        if (EnumText.ParseOptional<BrandTone>(request.Tone, "tone") is BrandTone tone) profile.Tone = tone;
        if (EnumText.ParseOptional<AddressForm>(request.AddressForm, "addressForm") is AddressForm form)
            profile.AddressForm = form;
        if (EnumText.ParseOptional<EmojiUsage>(request.EmojiUsage, "emojiUsage") is EmojiUsage emoji)
            profile.EmojiUsage = emoji;

        if (request.BannedPhrases is not null)
            profile.BannedPhrases = BrandProfileRules.BannedPhrases(request.BannedPhrases);
        if (request.DefaultHashtags is not null)
            profile.DefaultHashtags = BrandProfileRules.Hashtags(request.DefaultHashtags);
        if (request.TargetAudience is not null) profile.TargetAudience = TargetAudience(request.TargetAudience);
        if (request.ExtraContext is not null) profile.ExtraContext = ExtraContext(request.ExtraContext);
        if (request.SocialHandles is not null) profile.SocialHandles = await HandlesAsync(request.SocialHandles, ct);
        if (request.PrimaryColor is not null)
            profile.PrimaryColor = BrandProfileRules.Color(request.PrimaryColor, "ana renk");
        if (request.AccentColor is not null)
            profile.AccentColor = BrandProfileRules.Color(request.AccentColor, "vurgu rengi");
        if (request.IsDefault is bool isDefault) profile.IsDefault = isDefault;

        if (profile.IsDefault)
            await content.ClearDefaultBrandProfilesAsync(tenantId, profile.SiteId, profile.Id, ct);

        await content.SaveChangesAsync(ct);
        return BrandProfileDto.From(profile);
    }

    /// <summary>
    /// Profili ve logosunu siler. Profille uretilmis isler kalir, profil baglantisi kopar.
    /// Varsayilan silindiyse kapsam varsayilansiz kalir — yenisi kendiliginden secilmez.
    /// </summary>
    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var profile = await RequireAsync(id, tenantId, ct);
        var logo = profile.LogoStorageKey;

        content.RemoveBrandProfile(profile);
        await content.SaveChangesAsync(ct);

        if (logo is not null) await DeleteFileAsync(logo, ct);
    }

    /// <summary>
    /// Logoyu dogrular, PNG'ye normalize edip saklar; varsa eskisini siler. Her yukleme yeni
    /// anahtar alir — yarida kalan yuklemede eski logo bozulmaz.
    /// </summary>
    public async Task<BrandProfileDto> SetLogoAsync(
        Guid id, Guid tenantId, byte[] file, CancellationToken ct = default)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("Logo dosyası boş");
        if (file.Length > MaxLogoBytes)
            throw new InvalidOperationException($"Logo en fazla {MaxLogoBytes / (1024 * 1024)} MB olabilir");

        var profile = await RequireAsync(id, tenantId, ct);

        var logo = canvas.NormalizeLogo(file, LogoMaxSide)
            ?? throw new InvalidOperationException(
                "Logo okunamadı — PNG, JPEG ya da WebP yükleyin (SVG desteklenmiyor)");

        var key = $"{tenantId}/brand/{profile.Id}/logo-{Guid.CreateVersion7():N}.png";
        await storage.SaveAsync(key, logo.Content, ct);

        var previous = profile.LogoStorageKey;
        profile.LogoStorageKey = key;
        await content.SaveChangesAsync(ct);

        if (previous is not null) await DeleteFileAsync(previous, ct);
        return BrandProfileDto.From(profile);
    }

    public async Task<(byte[] Content, string ContentType)> GetLogoAsync(
        Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var profile = await RequireAsync(id, tenantId, ct);
        if (profile.LogoStorageKey is not { } key)
            throw new NotFoundException("Bu marka profilinin logosu yok");

        var bytes = await storage.ReadAsync(key, ct)
            ?? throw new NotFoundException("Logo dosyası bulunamadı");
        return (bytes, "image/png");
    }

    public async Task DeleteLogoAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var profile = await RequireAsync(id, tenantId, ct);
        if (profile.LogoStorageKey is not { } key) return;

        profile.LogoStorageKey = null;
        await content.SaveChangesAsync(ct);
        await DeleteFileAsync(key, ct);
    }

    public async Task<IReadOnlyList<PlatformProfileDto>> ListPlatformsAsync(
        bool onlyActive = true, CancellationToken ct = default) =>
        [.. (await content.ListPlatformProfilesAsync(onlyActive, ct)).Select(PlatformProfileDto.From)];

    private async Task<BrandProfile> RequireAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await content.GetBrandProfileAsync(id, tenantId, ct)
            ?? throw new NotFoundException($"Marka profili {id} bulunamadı");

    private async Task RequireSiteOrNullAsync(Guid? siteId, Guid tenantId, CancellationToken ct)
    {
        if (siteId is not Guid id || id == Guid.Empty) return;
        _ = await sites.GetSiteForTenantAsync(id, tenantId, ct)
            ?? throw new NotFoundException($"Site {id} bulunamadi");
    }

    /// <summary>Hesap adlari yalniz tanimli platformlar icin — pasif platformlar da gecerli.</summary>
    private async Task<Dictionary<string, string>> HandlesAsync(
        Dictionary<string, string?>? handles, CancellationToken ct)
    {
        if (handles is not { Count: > 0 }) return [];

        var codes = (await content.ListPlatformProfilesAsync(onlyActive: false, ct))
            .Select(p => p.Code)
            .ToHashSet(StringComparer.Ordinal);
        return BrandProfileRules.Handles(handles, codes);
    }

    /// <summary>Kayit gitti; dosya silinemezse yalniz diskte artik kalir, istek basarisiz sayilmaz.</summary>
    private async Task DeleteFileAsync(string key, CancellationToken ct)
    {
        try
        {
            await storage.DeleteAsync(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Logo dosyası silinemedi: {Key}", key);
        }
    }

    private static string? TargetAudience(string? value) =>
        BrandProfileRules.Text(value, BrandProfileRules.MaxTargetAudienceChars, "Hedef kitle");

    private static string? ExtraContext(string? value) =>
        BrandProfileRules.Text(value, BrandProfileRules.MaxExtraContextChars, "Ek bağlam");
}
