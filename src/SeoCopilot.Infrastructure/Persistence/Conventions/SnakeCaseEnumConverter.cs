using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SeoCopilot.Infrastructure.Persistence.Conventions;

/// <summary>
/// Enum &lt;-&gt; snake_case metin. 'DnsTxt' -> 'dns_txt', 'SatisOdakli' -> 'satis_odakli'.
/// Tum enum'lara ConfigureConventions icinde uygulanir.
/// </summary>
public sealed partial class SnakeCaseEnumConverter<TEnum>()
    : ValueConverter<TEnum, string>(v => ToSnake(v!.ToString()!), s => FromSnake(s))
    where TEnum : struct, Enum
{
    public static string ToSnake(string name) =>
        BoundaryRegex().Replace(name, "$1_$2").ToLowerInvariant();

    public static TEnum FromSnake(string value) =>
        Enum.Parse<TEnum>(value.Replace("_", string.Empty), ignoreCase: true);

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex BoundaryRegex();
}
