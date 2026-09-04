using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

public sealed class SchemaMissingRule : ISeoRule
{
    public string Code => "SCHEMA_MISSING";
    public RuleCategory Category => RuleCategory.StructuredData;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page) =>
        page.SchemaTypes.Count == 0 ? "Sayfada schema.org isaretlemesi bulunamadi." : null;
}

/// <summary>
/// Isaretleme var ama bicimi bozuk. En yaygin hali: tek &lt;script&gt; icine birden fazla kok
/// nesnenin arka arkaya konmasi — JSON-LD'ye gore gecersiz, ayri script'lere bolunmeli.
/// </summary>
public sealed class InvalidStructuredDataRule : ISeoRule
{
    public string Code => "INVALID_STRUCTURED_DATA";
    public RuleCategory Category => RuleCategory.StructuredData;
    public Severity Severity => Severity.Medium;
    public int Weight => 4;

    public string? Evaluate(PageInput page) =>
        page.InvalidSchemaBlocks > 0
            ? $"{page.InvalidSchemaBlocks} JSON-LD blogu gecersiz bicimde " +
              "(bozuk JSON ya da tek script icinde birden fazla kok nesne)."
            : null;
}

/// <summary>Paylasim kartlari icin gereken temel Open Graph etiketleri.</summary>
public sealed class OgTagsMissingRule : ISeoRule
{
    private static readonly string[] Required = ["og:title", "og:description", "og:image"];

    public string Code => "OG_TAGS_MISSING";
    public RuleCategory Category => RuleCategory.StructuredData;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page)
    {
        var present = new HashSet<string>(page.OgTags, StringComparer.OrdinalIgnoreCase);
        var missing = Required.Where(t => !present.Contains(t)).ToList();

        return missing.Count == 0
            ? null
            : $"Open Graph etiketleri eksik: {string.Join(", ", missing)}.";
    }
}

public sealed class LangAttrMissingRule : ISeoRule
{
    public string Code => "LANG_ATTR_MISSING";
    public RuleCategory Category => RuleCategory.I18n;
    public Severity Severity => Severity.Medium;
    public int Weight => 4;

    public string? Evaluate(PageInput page) =>
        string.IsNullOrWhiteSpace(page.Lang) ? "<html> etiketinde lang niteligi yok." : null;
}
