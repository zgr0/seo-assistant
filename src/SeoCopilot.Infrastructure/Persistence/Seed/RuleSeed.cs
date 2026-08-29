using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Infrastructure.Persistence.Seed;

/// <summary>rules tablosu seed'i. Kural motoru handler kodlariyla hizali.</summary>
internal sealed class RuleSeed : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> b)
    {
        b.HasData(
            R("HTTP_STATUS", RuleCategory.Indexability, Severity.Critical, 10,
                "Sayfa 2xx donmuyor", "Sayfa 4xx/5xx durum kodu donuyor ve dizine eklenemez.",
                "Sunucu/yonlendirme yapilandirmasini duzelt, kalici 200 don."),
            R("META_TITLE_MISSING", RuleCategory.Meta, Severity.Critical, 9,
                "Title etiketi yok", "Sayfada <title> etiketi bulunmuyor.",
                "Her sayfaya 30-60 karakter, anahtar kelime iceren benzersiz bir title ekle."),
            R("META_TITLE_LENGTH", RuleCategory.Meta, Severity.Medium, 5,
                "Title uzunlugu ideal degil", "Title 30 karakterden kisa veya 60 karakterden uzun.",
                "Title'i 30-60 karakter araligina getir."),
            R("META_DESCRIPTION_MISSING", RuleCategory.Meta, Severity.High, 6,
                "Meta description yok", "Sayfada meta description yok; SERP snippet'i kontrolsuz.",
                "70-160 karakter, tiklama tesvik eden bir meta description yaz."),
            R("META_DESCRIPTION_LENGTH", RuleCategory.Meta, Severity.Low, 3,
                "Meta description uzunlugu ideal degil", "Meta description 70 karakterden kisa veya 160 karakterden uzun.",
                "Uzunlugu 70-160 karakter araligina cek."),
            R("H1_MISSING", RuleCategory.Content, Severity.High, 6,
                "H1 yok", "Sayfada H1 basligi bulunmuyor.",
                "Sayfa basina tek ve aciklayici bir H1 ekle."),
            R("H1_MULTIPLE", RuleCategory.Content, Severity.Medium, 4,
                "Birden fazla H1", "Sayfada birden cok H1 var.",
                "Tek H1 birak, digerlerini H2/H3 yap."),
            R("CANONICAL_MISSING", RuleCategory.Indexability, Severity.Low, 3,
                "Canonical yok", "rel=canonical etiketi tanimli degil.",
                "Kendine referans veren bir canonical URL ekle."),
            R("THIN_CONTENT", RuleCategory.Content, Severity.Medium, 5,
                "Zayif icerik", "Sayfa metni 300 kelimenin altinda.",
                "Icerigi ozgun ve kullaniciya deger katacak sekilde genislet."),
            R("DUPLICATE_CONTENT", RuleCategory.Content, Severity.High, 7,
                "Yinelenen icerik", "Ayni content_hash birden fazla sayfada goruluyor.",
                "Icerigi farklilastir veya canonical ile asil sayfayi isaret et."),
            R("NOINDEX_DETECTED", RuleCategory.Indexability, Severity.Critical, 9,
                "noindex etiketi", "robots meta veya X-Robots-Tag noindex iceriyor.",
                "Dizine girmesi gereken sayfalarda noindex'i kaldir."),
            R("BROKEN_INTERNAL_LINK", RuleCategory.Links, Severity.High, 6,
                "Kirik ic link", "Ic link 4xx/5xx donen bir sayfaya gidiyor.",
                "Hedefi guncelle veya linki kaldir."),
            R("IMAGE_ALT_MISSING", RuleCategory.Images, Severity.Low, 3,
                "Alt metni eksik gorseller", "Bir veya daha fazla <img> alt niteligi tasimiyor.",
                "Anlamli gorsellere aciklayici alt metni ekle."),
            R("SLOW_TTFB", RuleCategory.Performance, Severity.Medium, 5,
                "Yavas TTFB", "Ilk bayt suresi 800 ms'nin uzerinde.",
                "Sunucu yanit suresini, cache ve CDN kullanimini iyilestir."),
            R("POOR_LCP", RuleCategory.Performance, Severity.High, 7,
                "Kotu LCP", "Largest Contentful Paint 2.5 sn'nin uzerinde.",
                "Kritik gorselleri onceliklendir, render-blocking kaynaklari azalt."),
            R("STRUCTURED_DATA_MISSING", RuleCategory.StructuredData, Severity.Low, 3,
                "Yapisal veri yok", "Sayfada schema.org isaretlemesi bulunamadi.",
                "Uygun schema tipini (Article, Product, FAQ...) JSON-LD olarak ekle."),
            R("HREFLANG_INVALID", RuleCategory.I18n, Severity.Medium, 4,
                "Gecersiz hreflang", "hreflang deger(ler)i gecersiz veya karsilikli degil.",
                "Dil-bolge kodlarini duzelt ve karsilikli hreflang tanimla.")
        );
    }

    private static Rule R(string code, RuleCategory category, Severity severity, int weight,
        string title, string description, string howToFix) => new()
    {
        Code = code,
        Category = category,
        Severity = severity,
        Weight = weight,
        TitleTr = title,
        DescriptionTr = description,
        HowToFixTr = howToFix,
        IsActive = true
    };
}
