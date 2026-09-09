using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities.Rules;

/// <summary>Kural katalogu — SEED verisi, koda gomulmez.</summary>
public class Rule
{
    /// <summary>Orn. 'META_TITLE_MISSING'.</summary>
    public string Code { get; set; } = string.Empty;

    public RuleCategory Category { get; set; }
    public Severity Severity { get; set; }

    /// <summary>1-10.</summary>
    public int Weight { get; set; }

    public string TitleTr { get; set; } = string.Empty;
    public string DescriptionTr { get; set; } = string.Empty;

    /// <summary>
    /// "Bu bulguyu yoksaymali miyim?" sorusunun cevabi — mesru istisnalar ya da acikca
    /// istisnasi olmadigi bilgisi. Bulgu detayinda Yoksay eyleminin yaninda gosterilir.
    /// </summary>
    public string? WhenToIgnoreTr { get; set; }

    public string HowToFixTr { get; set; } = string.Empty;

    /// <summary>
    /// Etkilenen sayfalar listesinin nasil okunacagi: hangi satirlar oncelikli, satirlar tek
    /// bir sablondan mi geliyor, tek tek mi toplu mu duzeltilir. Listenin ustunde gosterilir.
    /// </summary>
    public string? AffectedNoteTr { get; set; }

    public string? DocUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Issue> Issues { get; set; } = [];
}
