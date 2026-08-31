using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules;

/// <summary>Tek bir SEO kurali. Saf fonksiyon: ayni girdi -> ayni cikti, yan etki yok.</summary>
public interface ISeoRule
{
    /// <summary>Sabit kural kodu, orn. "META_TITLE_MISSING". rules.code ile birebir ayni olmali.</summary>
    string Code { get; }

    /// <summary>Skor kategorisi — crawl.category_scores bunun uzerinden hesaplanir.</summary>
    RuleCategory Category { get; }

    /// <summary>Bu kuralin ihlali ne kadar agir.</summary>
    Severity Severity { get; }

    /// <summary>rules.weight ile ayni deger; issues.weight'e yazilir.</summary>
    int Weight { get; }

    /// <summary>Kural gecerse null; ihlal varsa kullaniciya gosterilecek mesaj.</summary>
    string? Evaluate(PageInput page);
}
