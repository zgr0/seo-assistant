using System.Globalization;
using System.Text;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

/// <summary>
/// Anchor metni hedef sayfayi anlatmiyor ("buraya tiklayin", "read more").
/// Karsilastirma once ASCII'ye indirgenir, boylece "tiklayin"/"tıklayın" ayni sayilir.
/// </summary>
public sealed class GenericAnchorTextRule : ISeoRule
{
    private static readonly HashSet<string> Generic = new(StringComparer.OrdinalIgnoreCase)
    {
        "tikla", "tiklayin", "tiklayiniz", "buraya tikla", "buraya tiklayin", "buraya",
        "devam", "devami", "devamini oku", "daha fazla", "daha fazlasi", "detay", "detaylar",
        "oku", "incele", "goruntule", "link", "bu sayfa", "bu link",
        "click", "click here", "here", "read more", "more", "learn more", "this page", "this link"
    };

    public string Code => "GENERIC_ANCHOR_TEXT";
    public RuleCategory Category => RuleCategory.Links;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page)
    {
        var hits = page.InternalAnchorTexts
            .Select(Simplify)
            .Where(t => t.Length > 0 && Generic.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return hits.Count == 0
            ? null
            : $"{hits.Count} iç linkte açıklayıcı olmayan anchor metni var: {string.Join(", ", hits.Take(5))}.";
    }

    /// <summary>Kucuk harfe cevirir, aksanlari atar, sondaki noktalama isaretlerini siler.</summary>
    private static string Simplify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var lowered = text.Trim().ToLowerInvariant().Replace('ı', 'i');
        var decomposed = lowered.Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c) || c == ' ') sb.Append(c);
        }

        return sb.ToString().Trim();
    }
}
