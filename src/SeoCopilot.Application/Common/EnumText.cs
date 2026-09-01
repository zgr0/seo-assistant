namespace SeoCopilot.Application.Common;

/// <summary>
/// Enum'lari istemci metinlerinden okur. Hem <c>MetaDescription</c> hem <c>meta_description</c>
/// (ve <c>meta-description</c>) kabul edilir — veritabani gosterimi snake_case oldugundan
/// istemciler iki bicimi de gonderiyor.
/// </summary>
public static class EnumText
{
    public static bool TryParse<T>(string? value, out T parsed) where T : struct, Enum
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var compact = value.Replace("_", string.Empty).Replace("-", string.Empty).Trim();
        return Enum.TryParse(compact, ignoreCase: true, out parsed) && Enum.IsDefined(parsed);
    }

    /// <summary>Bos/eksik deger null doner; tanimsiz deger hata firlatir.</summary>
    public static T? ParseOptional<T>(string? value, string fieldName) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!TryParse<T>(value, out var parsed))
            throw new InvalidOperationException($"Gecersiz {fieldName}: '{value}'");
        return parsed;
    }

    public static T ParseRequired<T>(string? value, string fieldName) where T : struct, Enum =>
        ParseOptional<T>(value, fieldName)
            ?? throw new InvalidOperationException($"{fieldName} zorunlu");
}
