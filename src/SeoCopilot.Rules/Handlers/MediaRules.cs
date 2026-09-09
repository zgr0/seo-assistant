using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules.Handlers;

public sealed class ImageMissingAltRule : ISeoRule
{
    public string Code => "IMAGE_MISSING_ALT";
    public RuleCategory Category => RuleCategory.Images;
    public Severity Severity => Severity.Low;
    public int Weight => 3;

    public string? Evaluate(PageInput page) =>
        page.ImagesNoAlt > 0
            ? $"{page.ImagesTotal} görselin {page.ImagesNoAlt} tanesinde alt metni yok."
            : null;
}

/// <summary>
/// 200 KB'i asan gorseller. Yalniz boyutu olculebilen gorseller degerlendirilir;
/// crawler olcum yapmadiysa (<see cref="PageInput.ImageSizes"/> bos) kural sessizdir.
/// </summary>
public sealed class ImageTooLargeRule : ISeoRule
{
    public const long MaxBytes = 200 * 1024;

    public string Code => "IMAGE_TOO_LARGE";
    public RuleCategory Category => RuleCategory.Images;
    public Severity Severity => Severity.Medium;
    public int Weight => 4;

    public string? Evaluate(PageInput page)
    {
        var oversized = page.ImageSizes.Where(i => i.Bytes > MaxBytes).ToList();
        if (oversized.Count == 0) return null;

        var largest = oversized.Max(i => i.Bytes);
        return $"{oversized.Count} görsel {MaxBytes / 1024} KB'ı aşıyor (en büyüğü {largest / 1024} KB).";
    }
}
