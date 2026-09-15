namespace SeoCopilot.Application.Abstractions;

/// <summary>
/// Taranan sayfadaki bir gorseli indirir. Adres sayfa icerigidir, yani disaridan gelir:
/// ic aglara (localhost, 10.x, bulut meta veri adresi) ulasmamasi uygulamanin sorumlulugudur.
/// </summary>
public interface ISiteImageFetcher
{
    /// <summary>Gorsel degilse, cok buyukse ya da adrese izin verilmiyorsa null.</summary>
    Task<byte[]?> FetchAsync(string url, CancellationToken ct = default);
}
