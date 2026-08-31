using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

public sealed class ImageAltRule : ISeoRule
{
    public string Code => "IMAGE_ALT_MISSING";
    public RuleCategory Category => RuleCategory.Images;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page) =>
        page.ImagesNoAlt > 0
            ? $"{page.ImagesTotal} gorselin {page.ImagesNoAlt} tanesinde alt metni yok."
            : null;
}

public sealed class StructuredDataRule : ISeoRule
{
    public string Code => "STRUCTURED_DATA_MISSING";
    public RuleCategory Category => RuleCategory.StructuredData;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page) =>
        page.SchemaTypes.Count == 0 ? "Sayfada schema.org isaretlemesi bulunamadi." : null;
}
