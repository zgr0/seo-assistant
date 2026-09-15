using System.Text.Json;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services.Content;

namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Sitede daha once uretilen sosyal gonderiler. Iki soruya cevap verir: bir sayfadan kac kez
/// gonderi uretildi (sayfa sirasi ve aci bununla doner) ve bir metin daha once yazildi mi
/// (ayni govde ikinci kez kaydedilmez).
/// </summary>
public sealed class PostHistory
{
    /// <summary>Gecmis olarak okunacak azami is — cok eski gonderiler tekrar sayilmaz.</summary>
    public const int MaxRecords = 1000;

    public static readonly PostHistory Empty = new([]);

    private readonly Dictionary<string, int> _usageByPage = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _bodiesByPage = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _bodies = new(StringComparer.Ordinal);

    /// <param name="records">Yeniden eskiye sirali.</param>
    private PostHistory(IEnumerable<SocialPostRecord> records)
    {
        foreach (var record in records)
        {
            if (PageUrlOf(record) is { } url)
            {
                _usageByPage[url] = _usageByPage.GetValueOrDefault(url) + 1;

                if (!_bodiesByPage.TryGetValue(url, out var list))
                    _bodiesByPage[url] = list = [];
                list.AddRange(record.Bodies);
            }

            foreach (var body in record.Bodies)
                _bodies.Add(Normalize(body));
        }
    }

    /// <summary>Sayfadan yazilmis en yeni gonderi govdeleri.</summary>
    public IReadOnlyList<string> BodiesOf(string pageUrl, int take) =>
        _bodiesByPage.TryGetValue(pageUrl, out var list) ? [.. list.Take(take)] : [];

    /// <param name="exceptJobId">Yeniden denenen is kendi eski kaydini tekrar saymasin.</param>
    public static PostHistory From(IEnumerable<SocialPostRecord> records, Guid? exceptJobId = null) =>
        new(records.Where(r => r.JobId != exceptJobId));

    /// <summary>Sayfadan (tum platformlarda) daha once acilan gonderi isi sayisi.</summary>
    public int UsageOf(string pageUrl) => _usageByPage.GetValueOrDefault(pageUrl);

    /// <summary>Bu govde (bosluk ve buyuk/kucuk harf farki gozetilmeden) daha once yazildi mi.</summary>
    public bool Contains(string body) => _bodies.Contains(Normalize(body));

    public static string Normalize(string body) =>
        string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();

    /// <summary>Sayfa satiri sonraki taramalarda silinmis olabilir; is girdisindeki adres yedektir.</summary>
    private static string? PageUrlOf(SocialPostRecord record)
    {
        if (record.PageUrl is { Length: > 0 } url) return url;

        return ContentPrompt.ParseInput(record.Input) is JsonElement input
            && input.TryGetProperty("pageUrl", out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }
}
