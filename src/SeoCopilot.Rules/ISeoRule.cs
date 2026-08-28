using SeoCopilot.Domain.Enums;
using SeoCopilot.Rules.Model;

namespace SeoCopilot.Rules;

/// <summary>Tek bir SEO kurali. Saf fonksiyon: ayni girdi -> ayni cikti, yan etki yok.</summary>
public interface ISeoRule
{
    /// <summary>Sabit kural kodu, orn. "TITLE_MISSING".</summary>
    string Code { get; }

    /// <summary>Bu kuralin ihlali ne kadar agir.</summary>
    Severity Severity { get; }

    /// <summary>Kural gecerse null; ihlal varsa kullaniciya gosterilecek mesaj.</summary>
    string? Evaluate(PageInput page);
}
