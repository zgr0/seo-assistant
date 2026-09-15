using System.Text.Json;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Gonderi gorselinin sablonunu secer. Fotografli ve fotografsiz gorseller ayri sablon
/// havuzundan secilir; secim gonderi sirasiyla doner — ard arda gonderiler ayni gorunmez.
/// </summary>
public static class ImageTemplatePicker
{
    /// <summary>Is girdisinde (<c>content_jobs.input</c>) kullanicinin sectigi sablonlar.</summary>
    public const string InputKey = "imageTemplates";

    public static readonly ImageTemplate[] PhotoTemplates =
        [ImageTemplate.Overlay, ImageTemplate.Split, ImageTemplate.Framed, ImageTemplate.Label];

    public static readonly ImageTemplate[] CardTemplates = [ImageTemplate.Poster, ImageTemplate.Quote];

    /// <param name="allowed">Izin verilen sablonlar; bos ise hepsi.</param>
    /// <param name="photo">Zemin gercek bir fotograf mi (site ya da AI) yoksa marka karti mi.</param>
    /// <param name="hasSubline">Alinti sablonu basacak bir cumle olmadan kullanilmaz.</param>
    /// <param name="index">Gonderi sirasi — ayni sayfanin/sitenin gonderileri farkli sablon alsin.</param>
    public static ImageTemplate Pick(
        IReadOnlyCollection<ImageTemplate> allowed, bool photo, bool hasSubline, int index)
    {
        var usable = (allowed.Count == 0 ? [.. PhotoTemplates, .. CardTemplates] : allowed)
            .Where(t => t != ImageTemplate.Quote || hasSubline)
            .ToList();

        var pool = usable.Where(t => (photo ? PhotoTemplates : CardTemplates).Contains(t)).ToList();

        // Fotograf bulunamadi ama kullanici yalniz fotografli sablon sectiyse secimine uyulur:
        // bu sablonlar marka karti zemininde de okunur (panel, cerceve, etiket karti).
        if (pool.Count == 0 && !photo) pool = [.. usable.Where(t => PhotoTemplates.Contains(t))];

        if (pool.Count == 0) return photo ? ImageTemplate.Overlay : ImageTemplate.Poster;

        return pool[Math.Abs(index) % pool.Count];
    }

    /// <summary>
    /// Yalniz fotografsiz sablonlar secildiyse fotograf aranmaz — afis ve alinti fotograf ustunde
    /// okunmaz, zemin marka karti olmalidir.
    /// </summary>
    public static bool WantsCardOnly(IReadOnlyCollection<ImageTemplate> allowed) =>
        allowed.Count > 0 && allowed.All(t => CardTemplates.Contains(t));

    /// <summary>Istekteki sablon adlari (buyuk/kucuk harf duyarsiz). Bilinmeyen ad 400 doner.</summary>
    public static IReadOnlyList<ImageTemplate> Parse(IEnumerable<string>? names)
    {
        var result = new List<ImageTemplate>();
        foreach (var name in names ?? [])
        {
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (!Enum.TryParse<ImageTemplate>(name.Trim(), ignoreCase: true, out var template)
                || !Enum.IsDefined(template))
            {
                throw new InvalidOperationException($"Bilinmeyen görsel şablonu: '{name}'");
            }

            if (!result.Contains(template)) result.Add(template);
        }

        return result;
    }

    /// <summary>Is girdisinden kullanicinin sectigi sablonlar; yoksa bos.</summary>
    public static IReadOnlyList<ImageTemplate> FromInput(JsonElement? input)
    {
        if (input is not JsonElement el
            || el.ValueKind != JsonValueKind.Object
            || !el.TryGetProperty(InputKey, out var list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return [.. list.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => Enum.TryParse<ImageTemplate>(e.GetString(), ignoreCase: true, out var t) ? t : (ImageTemplate?)null)
            .OfType<ImageTemplate>()
            .Distinct()];
    }
}
