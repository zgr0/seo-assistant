using Sitecraft.Application.Abstractions;
using Sitecraft.Application.Common;

namespace Sitecraft.Application.Services;

/// <summary>
/// Bir uretim isinin hangi marka profiliyle calisacagini belirler. Is acilirken bir kez cozulur
/// ve ise yazilir — gecmiste hangi profilin kullanildigi boylece gorunur.
/// </summary>
public sealed class BrandProfileResolver(IContentRepository content)
{
    /// <param name="siteId">Isin sitesi; sayfasiz islerde null (yalniz kiraci varsayilani aranir).</param>
    /// <param name="requestedId">
    /// null: sitenin varsayilani, yoksa kiracinin varsayilani, o da yoksa profilsiz.
    /// <c>Guid.Empty</c>: kullanici acikca profilsiz istedi. Dolu: o profil — kiraciya ait olmali
    /// ve baska bir siteye bagli olmamali.
    /// </param>
    public async Task<Guid?> ResolveAsync(
        Guid tenantId, Guid? siteId, Guid? requestedId, CancellationToken ct = default)
    {
        if (requestedId == Guid.Empty) return null;

        if (requestedId is Guid id)
        {
            var profile = await content.GetBrandProfileAsync(id, tenantId, ct)
                ?? throw new NotFoundException($"Marka profili {id} bulunamadı");

            if (siteId is Guid site && profile.SiteId is Guid owner && owner != site)
                throw new InvalidOperationException($"'{profile.Name}' marka profili başka bir siteye ait");

            return profile.Id;
        }

        return (await content.FindDefaultBrandProfileAsync(tenantId, siteId, ct))?.Id;
    }
}
