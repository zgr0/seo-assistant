using System.Text.Json;
using SeoCopilot.Domain.Entities.Content;

namespace SeoCopilot.Application.Services.Content;

/// <summary>
/// Model yanitini <c>content_variants</c> satirlarina cevirir. Yanit JSON degilse ya da
/// beklenen semaya uymuyorsa uretim bosa gitmesin diye ham metin tek varyant olarak yazilir.
/// </summary>
public static class ContentResponseParser
{
    public const int MaxVariants = 10;

    public static List<ContentVariant> Parse(string response)
    {
        var variants = new List<ContentVariant>();
        if (string.IsNullOrWhiteSpace(response)) return variants;

        var json = ExtractJson(response);
        if (json is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (var item in ItemsOf(doc.RootElement).Take(MaxVariants))
                {
                    var variant = ToVariant(item, variants.Count);
                    if (variant is not null) variants.Add(variant);
                }
            }
            catch (JsonException)
            {
                // Sema disi yanit — asagidaki ham metin yedegine duser.
            }
        }

        if (variants.Count == 0)
            variants.Add(NewVariant(0, null, response.Trim(), [], null));

        return variants;
    }

    private static IEnumerable<JsonElement> ItemsOf(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root.EnumerateArray();

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("variants", out var variants) && variants.ValueKind == JsonValueKind.Array)
                return variants.EnumerateArray();
            return [root];
        }

        return [];
    }

    private static ContentVariant? ToVariant(JsonElement item, int index)
    {
        if (item.ValueKind == JsonValueKind.String)
        {
            var text = item.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : NewVariant(index, null, text, [], null);
        }

        if (item.ValueKind != JsonValueKind.Object) return null;

        var body = Text(item, "body") ?? Text(item, "text") ?? Text(item, "content") ?? Text(item, "title");
        var hashtags = Strings(item, "hashtags");

        // HashtagSet uretimlerinde govde bos gelebilir; hashtag listesi tek basina anlamli.
        if (string.IsNullOrWhiteSpace(body) && hashtags.Count > 0) body = string.Join(" ", hashtags);
        if (string.IsNullOrWhiteSpace(body)) return null;

        return NewVariant(index, Text(item, "angle"), body, hashtags, Text(item, "cta"));
    }

    private static ContentVariant NewVariant(
        int index, string? angle, string body, List<string> hashtags, string? cta) => new()
        {
            VariantIndex = index,
            Angle = Clip(angle, 64),
            Body = body.Trim(),
            Hashtags = hashtags,
            Cta = Clip(cta, 256),
            CharCount = body.Trim().Length
        };

    /// <summary>Kod bloklarini ve on/arka aciklamalari atarak ilk JSON govdesini bulur.</summary>
    private static string? ExtractJson(string response)
    {
        var text = response.Trim();

        var fence = text.IndexOf("```", StringComparison.Ordinal);
        if (fence >= 0)
        {
            var start = text.IndexOf('\n', fence);
            var end = text.IndexOf("```", fence + 3, StringComparison.Ordinal);
            if (start > 0 && end > start) text = text[(start + 1)..end].Trim();
        }

        var objectStart = text.IndexOf('{');
        var arrayStart = text.IndexOf('[');
        var from = objectStart < 0 ? arrayStart : arrayStart < 0 ? objectStart : Math.Min(objectStart, arrayStart);
        if (from < 0) return null;

        var to = text[from] == '{' ? text.LastIndexOf('}') : text.LastIndexOf(']');
        return to > from ? text[from..(to + 1)] : null;
    }

    private static string? Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static List<string> Strings(JsonElement item, string name)
    {
        var result = new List<string>();
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array) return result;

        foreach (var element in value.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String) continue;
            var text = element.GetString();
            if (!string.IsNullOrWhiteSpace(text)) result.Add(text.Trim());
        }
        return result;
    }

    private static string? Clip(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : value.Trim()[..max];
}
