using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SeoCopilot.Infrastructure.Persistence.Conventions;

/// <summary>Dictionary&lt;string,T&gt; alanlarini jsonb'ye seri/deseri eder + degisiklik izleme.</summary>
public static class JsonbDictionary
{
    public static ValueConverter<Dictionary<string, T>, string> Converter<T>() => new(
        v => JsonSerializer.Serialize(v, JsonOpts),
        s => JsonSerializer.Deserialize<Dictionary<string, T>>(s, JsonOpts) ?? new());

    public static ValueComparer<Dictionary<string, T>> Comparer<T>() => new(
        (a, b) => JsonSerializer.Serialize(a, JsonOpts) == JsonSerializer.Serialize(b, JsonOpts),
        v => v == null ? 0 : JsonSerializer.Serialize(v, JsonOpts).GetHashCode(),
        v => JsonSerializer.Deserialize<Dictionary<string, T>>(JsonSerializer.Serialize(v, JsonOpts), JsonOpts)!);

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
}
