using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Infrastructure.Persistence.Seed;

/// <summary>
/// rules tablosu seed'i. Kural motoru handler kodlariyla birebir hizali olmak zorunda —
/// issues.rule_code buraya FK.
/// </summary>
internal sealed class RuleSeed : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> b)
    {
        b.HasData(
            // --- Indexability ---
            R("ROBOTS_NOINDEX", RuleCategory.Indexability, Severity.Critical, 9,
                "noindex etiketi", "robots meta veya X-Robots-Tag noindex iceriyor.",
                "Dizine girmesi gereken sayfalarda noindex'i kaldir."),
            R("BLOCKED_BY_ROBOTS_TXT", RuleCategory.Indexability, Severity.High, 7,
                "robots.txt engeli", "robots.txt taranmasi gereken ic adresleri kapatiyor.",
                "Disallow kurallarini daralt; yalnizca gercekten gizlenmesi gereken yollari kapat."),
            R("BROKEN_PAGE_4XX", RuleCategory.Indexability, Severity.Critical, 10,
                "Sayfa 4xx donuyor", "Sayfa 4xx durum kodu donuyor ve dizine eklenemez.",
                "Icerigi geri getir veya kalici olarak dogru adrese 301 yonlendir."),
            R("SERVER_ERROR_5XX", RuleCategory.Indexability, Severity.Critical, 10,
                "Sunucu hatasi", "Sayfa 5xx donuyor ya da hic getirilemedi.",
                "Sunucu hatasini gider; getirilemeyen sayfalar dizinden dusuyor."),
            R("REDIRECT_CHAIN", RuleCategory.Indexability, Severity.Medium, 5,
                "Yonlendirme zinciri", "Sayfaya birden fazla yonlendirme atlayarak ulasiliyor.",
                "Linkleri son adrese guncelle, zinciri tek atlamaya indir."),
            R("CANONICAL_MISSING", RuleCategory.Indexability, Severity.Low, 3,
                "Canonical yok", "rel=canonical etiketi tanimli degil.",
                "Kendine referans veren bir canonical URL ekle."),
            R("CANONICAL_POINTS_ELSEWHERE", RuleCategory.Indexability, Severity.Medium, 5,
                "Canonical baskasini gosteriyor", "rel=canonical sayfanin kendi adresini gostermiyor.",
                "Asil sayfa buysa canonical'i kendine cevir; degilse yinelenen icerigi kaldir."),
            R("SITEMAP_MISSING", RuleCategory.Indexability, Severity.Medium, 5,
                "Sitemap yok", "Sitede okunabilir bir sitemap bulunamadi.",
                "sitemap.xml yayinla ve robots.txt icinde Sitemap satiriyla bildir."),
            R("PAGE_NOT_IN_SITEMAP", RuleCategory.Indexability, Severity.Low, 3,
                "Sayfa sitemap disinda", "Dizinlenebilir sayfa sitemap'te listelenmiyor.",
                "Sayfayi sitemap'e ekle veya dizine girmemesi gerekiyorsa noindex ver."),

            // --- Meta ---
            R("META_TITLE_MISSING", RuleCategory.Meta, Severity.Critical, 9,
                "Title etiketi yok", "Sayfada <title> etiketi bulunmuyor.",
                "Her sayfaya 30-60 karakter, anahtar kelime iceren benzersiz bir title ekle."),
            R("META_TITLE_TOO_SHORT", RuleCategory.Meta, Severity.Medium, 5,
                "Title cok kisa", "Title 30 karakterden kisa.",
                "Title'i 30-60 karakter araligina cikar."),
            R("META_TITLE_TOO_LONG", RuleCategory.Meta, Severity.Medium, 5,
                "Title cok uzun", "Title 60 karakterden uzun; SERP'te kirpilir.",
                "Title'i 60 karakterin altina indir."),
            R("META_TITLE_DUPLICATE", RuleCategory.Meta, Severity.Medium, 5,
                "Yinelenen title", "Ayni title birden fazla sayfada kullaniliyor.",
                "Her sayfaya icerigini anlatan benzersiz bir title yaz."),
            R("META_DESC_MISSING", RuleCategory.Meta, Severity.High, 6,
                "Meta description yok", "Sayfada meta description yok; SERP snippet'i kontrolsuz.",
                "Tiklama tesvik eden, 160 karakteri asmayan bir meta description yaz."),
            R("META_DESC_TOO_LONG", RuleCategory.Meta, Severity.Low, 3,
                "Meta description cok uzun", "Meta description 160 karakterden uzun.",
                "Uzunlugu 160 karakterin altina cek."),
            R("META_DESC_DUPLICATE", RuleCategory.Meta, Severity.Low, 3,
                "Yinelenen meta description", "Ayni meta description birden fazla sayfada kullaniliyor.",
                "Her sayfa icin ayri bir aciklama yaz."),

            // --- Content ---
            R("H1_MISSING", RuleCategory.Content, Severity.High, 6,
                "H1 yok", "Sayfada H1 basligi bulunmuyor.",
                "Sayfa basina tek ve aciklayici bir H1 ekle."),
            R("H1_MULTIPLE", RuleCategory.Content, Severity.Medium, 4,
                "Birden fazla H1", "Sayfada birden cok H1 var.",
                "Tek H1 birak, digerlerini H2/H3 yap."),
            R("THIN_CONTENT", RuleCategory.Content, Severity.Medium, 5,
                "Zayif icerik", "Sayfa metni 300 kelimenin altinda.",
                "Icerigi ozgun ve kullaniciya deger katacak sekilde genislet."),
            R("DUPLICATE_CONTENT", RuleCategory.Content, Severity.High, 7,
                "Yinelenen icerik", "Ayni content_hash birden fazla sayfada goruluyor.",
                "Icerigi farklilastir veya canonical ile asil sayfayi isaret et."),
            R("HEADING_HIERARCHY_BROKEN", RuleCategory.Content, Severity.Low, 3,
                "Baslik hiyerarsisi bozuk", "Baslik seviyeleri atlanmis (orn. h2'den sonra h4).",
                "Basliklari sirayla kullan; seviye atlamadan h1 → h2 → h3 ilerle."),

            // --- Links ---
            R("BROKEN_INTERNAL_LINK", RuleCategory.Links, Severity.High, 6,
                "Kirik ic link", "Ic link 4xx/5xx donen bir sayfaya gidiyor.",
                "Hedefi guncelle veya linki kaldir."),
            R("ORPHAN_PAGE", RuleCategory.Links, Severity.Medium, 4,
                "Oksuz sayfa", "Sayfaya hicbir ic link isaret etmiyor.",
                "Ilgili sayfalardan ic link ver; menu veya icerik icinden erisilebilir yap."),
            R("TOO_DEEP", RuleCategory.Links, Severity.Low, 3,
                "Cok derin sayfa", "Sayfa kok sayfadan 4 tiklamadan uzakta.",
                "Site yapisini duzlestir; onemli sayfalari ust seviyelere yaklastir."),
            R("GENERIC_ANCHOR_TEXT", RuleCategory.Links, Severity.Low, 3,
                "Aciklayici olmayan anchor", "Ic linklerde \"buraya tiklayin\" gibi genel anchor metinleri var.",
                "Anchor metnini hedef sayfanin konusunu anlatacak sekilde yaz."),

            // --- Images ---
            R("IMAGE_MISSING_ALT", RuleCategory.Images, Severity.Low, 3,
                "Alt metni eksik gorseller", "Bir veya daha fazla <img> alt niteligi tasimiyor.",
                "Anlamli gorsellere aciklayici alt metni ekle."),
            R("IMAGE_TOO_LARGE", RuleCategory.Images, Severity.Medium, 4,
                "Buyuk gorsel", "Sayfadaki bir veya daha fazla gorsel 200 KB'i asiyor.",
                "Gorselleri sikistir, WebP/AVIF kullan ve boyutu ekrana gore olceklendir."),

            // --- Structured data & i18n ---
            R("SCHEMA_MISSING", RuleCategory.StructuredData, Severity.Low, 3,
                "Yapisal veri yok", "Sayfada schema.org isaretlemesi bulunamadi.",
                "Uygun schema tipini (Article, Product, FAQ...) JSON-LD olarak ekle."),
            R("OG_TAGS_MISSING", RuleCategory.StructuredData, Severity.Low, 3,
                "Open Graph etiketleri eksik", "og:title, og:description veya og:image tanimli degil.",
                "Paylasim kartlari icin temel Open Graph etiketlerini ekle."),
            R("LANG_ATTR_MISSING", RuleCategory.I18n, Severity.Medium, 4,
                "lang niteligi yok", "<html> etiketinde lang niteligi bulunmuyor.",
                "Sayfanin dilini <html lang=\"tr\"> seklinde bildir."),

            // --- Performance (PSI) ---
            R("LCP_POOR", RuleCategory.Performance, Severity.High, 7,
                "Kotu LCP", "Largest Contentful Paint 4 sn'nin uzerinde.",
                "Kritik gorselleri onceliklendir, render-blocking kaynaklari azalt."),
            R("CLS_POOR", RuleCategory.Performance, Severity.Medium, 5,
                "Kotu CLS", "Cumulative Layout Shift 0.25'in uzerinde.",
                "Gorsel/reklam alanlarina sabit boyut ver, gec yuklenen icerigi yer tutucuyla yerlestir."),
            R("INP_POOR", RuleCategory.Performance, Severity.Medium, 5,
                "Kotu INP", "Interaction to Next Paint 500 ms'nin uzerinde.",
                "Uzun JS gorevlerini bol, ana is parcacigini serbest birak.")
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
