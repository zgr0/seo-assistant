using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services;

/// <summary>Marka sesi profilleri ve platform seed listesi.</summary>
public sealed class BrandProfileService(IContentRepository content, ISiteRepository sites)
{
    public async Task<IReadOnlyList<BrandProfileDto>> ListAsync(
        Guid tenantId, Guid? siteId, CancellationToken ct = default) =>
        [.. (await content.ListBrandProfilesAsync(tenantId, siteId, ct)).Select(BrandProfileDto.From)];

    public async Task<BrandProfileDto> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        BrandProfileDto.From(await RequireAsync(id, tenantId, ct));

    public async Task<BrandProfileDto> CreateAsync(
        CreateBrandProfileRequest request, Guid tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Profil adı zorunlu");

        await RequireSiteOrNullAsync(request.SiteId, tenantId, ct);

        var profile = new BrandProfile
        {
            TenantId = tenantId,
            SiteId = request.SiteId,
            Name = request.Name.Trim(),
            Tone = EnumText.ParseOptional<BrandTone>(request.Tone, "tone") ?? BrandTone.Kurumsal,
            AddressForm = EnumText.ParseOptional<AddressForm>(request.AddressForm, "addressForm") ?? AddressForm.Siz,
            EmojiUsage = EnumText.ParseOptional<EmojiUsage>(request.EmojiUsage, "emojiUsage") ?? EmojiUsage.None,
            BannedPhrases = request.BannedPhrases ?? [],
            DefaultHashtags = request.DefaultHashtags ?? [],
            TargetAudience = Trim(request.TargetAudience),
            ExtraContext = Trim(request.ExtraContext),
            IsDefault = request.IsDefault ?? false
        };

        await content.AddBrandProfileAsync(profile, ct);
        await content.SaveChangesAsync(ct);

        if (profile.IsDefault)
        {
            await content.ClearDefaultBrandProfilesAsync(tenantId, profile.SiteId, profile.Id, ct);
            await content.SaveChangesAsync(ct);
        }

        return BrandProfileDto.From(profile);
    }

    public async Task<BrandProfileDto> UpdateAsync(
        Guid id, Guid tenantId, UpdateBrandProfileRequest request, CancellationToken ct = default)
    {
        var profile = await RequireAsync(id, tenantId, ct);

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new InvalidOperationException("Profil adı boş olamaz");
            profile.Name = request.Name.Trim();
        }

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

        if (request.BannedPhrases is not null) profile.BannedPhrases = [.. request.BannedPhrases];
        if (request.DefaultHashtags is not null) profile.DefaultHashtags = [.. request.DefaultHashtags];
        if (request.TargetAudience is not null) profile.TargetAudience = Trim(request.TargetAudience);
        if (request.ExtraContext is not null) profile.ExtraContext = Trim(request.ExtraContext);
        if (request.IsDefault is bool isDefault) profile.IsDefault = isDefault;

        if (profile.IsDefault)
            await content.ClearDefaultBrandProfilesAsync(tenantId, profile.SiteId, profile.Id, ct);

        await content.SaveChangesAsync(ct);
        return BrandProfileDto.From(profile);
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

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
