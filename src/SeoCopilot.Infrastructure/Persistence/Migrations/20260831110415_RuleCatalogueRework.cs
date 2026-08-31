using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RuleCatalogueRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Once yeni kurallar eklenir, sonra mevcut issues satirlari yeni kodlara tasinir;
            // eski kural satirlari ancak ondan sonra silinebilir (issues.rule_code → rules.code, NO ACTION).
            migrationBuilder.InsertData(
                table: "rules",
                columns: new[] { "code", "category", "description_tr", "doc_url", "how_to_fix_tr", "is_active", "severity", "title_tr", "weight" },
                values: new object[,]
                {
                    { "BLOCKED_BY_ROBOTS_TXT", "indexability", "robots.txt taranmasi gereken ic adresleri kapatiyor.", null, "Disallow kurallarini daralt; yalnizca gercekten gizlenmesi gereken yollari kapat.", true, "high", "robots.txt engeli", 7 },
                    { "BROKEN_PAGE_4XX", "indexability", "Sayfa 4xx durum kodu donuyor ve dizine eklenemez.", null, "Icerigi geri getir veya kalici olarak dogru adrese 301 yonlendir.", true, "critical", "Sayfa 4xx donuyor", 10 },
                    { "CANONICAL_POINTS_ELSEWHERE", "indexability", "rel=canonical sayfanin kendi adresini gostermiyor.", null, "Asil sayfa buysa canonical'i kendine cevir; degilse yinelenen icerigi kaldir.", true, "medium", "Canonical baskasini gosteriyor", 5 },
                    { "CLS_POOR", "performance", "Cumulative Layout Shift 0.25'in uzerinde.", null, "Gorsel/reklam alanlarina sabit boyut ver, gec yuklenen icerigi yer tutucuyla yerlestir.", true, "medium", "Kotu CLS", 5 },
                    { "GENERIC_ANCHOR_TEXT", "links", "Ic linklerde \"buraya tiklayin\" gibi genel anchor metinleri var.", null, "Anchor metnini hedef sayfanin konusunu anlatacak sekilde yaz.", true, "low", "Aciklayici olmayan anchor", 3 },
                    { "HEADING_HIERARCHY_BROKEN", "content", "Baslik seviyeleri atlanmis (orn. h2'den sonra h4).", null, "Basliklari sirayla kullan; seviye atlamadan h1 → h2 → h3 ilerle.", true, "low", "Baslik hiyerarsisi bozuk", 3 },
                    { "IMAGE_MISSING_ALT", "images", "Bir veya daha fazla <img> alt niteligi tasimiyor.", null, "Anlamli gorsellere aciklayici alt metni ekle.", true, "low", "Alt metni eksik gorseller", 3 },
                    { "IMAGE_TOO_LARGE", "images", "Sayfadaki bir veya daha fazla gorsel 200 KB'i asiyor.", null, "Gorselleri sikistir, WebP/AVIF kullan ve boyutu ekrana gore olceklendir.", true, "medium", "Buyuk gorsel", 4 },
                    { "INP_POOR", "performance", "Interaction to Next Paint 500 ms'nin uzerinde.", null, "Uzun JS gorevlerini bol, ana is parcacigini serbest birak.", true, "medium", "Kotu INP", 5 },
                    { "LANG_ATTR_MISSING", "i18n", "<html> etiketinde lang niteligi bulunmuyor.", null, "Sayfanin dilini <html lang=\"tr\"> seklinde bildir.", true, "medium", "lang niteligi yok", 4 },
                    { "LCP_POOR", "performance", "Largest Contentful Paint 4 sn'nin uzerinde.", null, "Kritik gorselleri onceliklendir, render-blocking kaynaklari azalt.", true, "high", "Kotu LCP", 7 },
                    { "META_DESC_DUPLICATE", "meta", "Ayni meta description birden fazla sayfada kullaniliyor.", null, "Her sayfa icin ayri bir aciklama yaz.", true, "low", "Yinelenen meta description", 3 },
                    { "META_DESC_MISSING", "meta", "Sayfada meta description yok; SERP snippet'i kontrolsuz.", null, "Tiklama tesvik eden, 160 karakteri asmayan bir meta description yaz.", true, "high", "Meta description yok", 6 },
                    { "META_DESC_TOO_LONG", "meta", "Meta description 160 karakterden uzun.", null, "Uzunlugu 160 karakterin altina cek.", true, "low", "Meta description cok uzun", 3 },
                    { "META_TITLE_DUPLICATE", "meta", "Ayni title birden fazla sayfada kullaniliyor.", null, "Her sayfaya icerigini anlatan benzersiz bir title yaz.", true, "medium", "Yinelenen title", 5 },
                    { "META_TITLE_TOO_LONG", "meta", "Title 60 karakterden uzun; SERP'te kirpilir.", null, "Title'i 60 karakterin altina indir.", true, "medium", "Title cok uzun", 5 },
                    { "META_TITLE_TOO_SHORT", "meta", "Title 30 karakterden kisa.", null, "Title'i 30-60 karakter araligina cikar.", true, "medium", "Title cok kisa", 5 },
                    { "OG_TAGS_MISSING", "structured_data", "og:title, og:description veya og:image tanimli degil.", null, "Paylasim kartlari icin temel Open Graph etiketlerini ekle.", true, "low", "Open Graph etiketleri eksik", 3 },
                    { "ORPHAN_PAGE", "links", "Sayfaya hicbir ic link isaret etmiyor.", null, "Ilgili sayfalardan ic link ver; menu veya icerik icinden erisilebilir yap.", true, "medium", "Oksuz sayfa", 4 },
                    { "PAGE_NOT_IN_SITEMAP", "indexability", "Dizinlenebilir sayfa sitemap'te listelenmiyor.", null, "Sayfayi sitemap'e ekle veya dizine girmemesi gerekiyorsa noindex ver.", true, "low", "Sayfa sitemap disinda", 3 },
                    { "REDIRECT_CHAIN", "indexability", "Sayfaya birden fazla yonlendirme atlayarak ulasiliyor.", null, "Linkleri son adrese guncelle, zinciri tek atlamaya indir.", true, "medium", "Yonlendirme zinciri", 5 },
                    { "ROBOTS_NOINDEX", "indexability", "robots meta veya X-Robots-Tag noindex iceriyor.", null, "Dizine girmesi gereken sayfalarda noindex'i kaldir.", true, "critical", "noindex etiketi", 9 },
                    { "SCHEMA_MISSING", "structured_data", "Sayfada schema.org isaretlemesi bulunamadi.", null, "Uygun schema tipini (Article, Product, FAQ...) JSON-LD olarak ekle.", true, "low", "Yapisal veri yok", 3 },
                    { "SERVER_ERROR_5XX", "indexability", "Sayfa 5xx donuyor ya da hic getirilemedi.", null, "Sunucu hatasini gider; getirilemeyen sayfalar dizinden dusuyor.", true, "critical", "Sunucu hatasi", 10 },
                    { "SITEMAP_MISSING", "indexability", "Sitede okunabilir bir sitemap bulunamadi.", null, "sitemap.xml yayinla ve robots.txt icinde Sitemap satiriyla bildir.", true, "medium", "Sitemap yok", 5 },
                    { "TOO_DEEP", "links", "Sayfa kok sayfadan 4 tiklamadan uzakta.", null, "Site yapisini duzlestir; onemli sayfalari ust seviyelere yaklastir.", true, "low", "Cok derin sayfa", 3 }
                });

            // Gecmis bulgulari yeni kodlara tasi. Karsiligi olmayanlar (SLOW_TTFB, HREFLANG_INVALID,
            // esik alti META_DESCRIPTION_LENGTH) silinir — o kurallar artik uretilmiyor.
            migrationBuilder.Sql("""
                UPDATE issues SET rule_code = 'ROBOTS_NOINDEX'    WHERE rule_code = 'NOINDEX_DETECTED';
                UPDATE issues SET rule_code = 'IMAGE_MISSING_ALT' WHERE rule_code = 'IMAGE_ALT_MISSING';
                UPDATE issues SET rule_code = 'META_DESC_MISSING' WHERE rule_code = 'META_DESCRIPTION_MISSING';
                UPDATE issues SET rule_code = 'SCHEMA_MISSING'    WHERE rule_code = 'STRUCTURED_DATA_MISSING';
                UPDATE issues SET rule_code = 'LCP_POOR'          WHERE rule_code = 'POOR_LCP';

                UPDATE issues AS i
                   SET rule_code = CASE WHEN p.status_code BETWEEN 400 AND 499
                                        THEN 'BROKEN_PAGE_4XX' ELSE 'SERVER_ERROR_5XX' END
                  FROM pages AS p
                 WHERE i.page_id = p.id AND i.rule_code = 'HTTP_STATUS';
                UPDATE issues SET rule_code = 'SERVER_ERROR_5XX' WHERE rule_code = 'HTTP_STATUS';

                UPDATE issues AS i
                   SET rule_code = CASE WHEN COALESCE(p.title_length, 0) < 30
                                        THEN 'META_TITLE_TOO_SHORT' ELSE 'META_TITLE_TOO_LONG' END
                  FROM pages AS p
                 WHERE i.page_id = p.id AND i.rule_code = 'META_TITLE_LENGTH';

                UPDATE issues AS i
                   SET rule_code = 'META_DESC_TOO_LONG'
                  FROM pages AS p
                 WHERE i.page_id = p.id AND i.rule_code = 'META_DESCRIPTION_LENGTH'
                   AND COALESCE(p.meta_desc_length, 0) > 160;

                DELETE FROM issues
                 WHERE rule_code IN ('META_TITLE_LENGTH', 'META_DESCRIPTION_LENGTH', 'SLOW_TTFB', 'HREFLANG_INVALID');

                UPDATE issue_ignores SET rule_code = 'ROBOTS_NOINDEX'    WHERE rule_code = 'NOINDEX_DETECTED';
                UPDATE issue_ignores SET rule_code = 'IMAGE_MISSING_ALT' WHERE rule_code = 'IMAGE_ALT_MISSING';
                UPDATE issue_ignores SET rule_code = 'META_DESC_MISSING' WHERE rule_code = 'META_DESCRIPTION_MISSING';
                UPDATE issue_ignores SET rule_code = 'SCHEMA_MISSING'    WHERE rule_code = 'STRUCTURED_DATA_MISSING';
                UPDATE issue_ignores SET rule_code = 'LCP_POOR'          WHERE rule_code = 'POOR_LCP';
                """);

            foreach (var retired in new[]
            {
                "HREFLANG_INVALID", "HTTP_STATUS", "IMAGE_ALT_MISSING", "META_DESCRIPTION_LENGTH",
                "META_DESCRIPTION_MISSING", "META_TITLE_LENGTH", "NOINDEX_DETECTED", "POOR_LCP",
                "SLOW_TTFB", "STRUCTURED_DATA_MISSING"
            })
            {
                migrationBuilder.DeleteData(table: "rules", keyColumn: "code", keyValue: retired);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BLOCKED_BY_ROBOTS_TXT");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_PAGE_4XX");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_POINTS_ELSEWHERE");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CLS_POOR");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "GENERIC_ANCHOR_TEXT");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "HEADING_HIERARCHY_BROKEN");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_MISSING_ALT");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_TOO_LARGE");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INP_POOR");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LANG_ATTR_MISSING");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LCP_POOR");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_DUPLICATE");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_MISSING");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_TOO_LONG");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_DUPLICATE");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_LONG");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_SHORT");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "OG_TAGS_MISSING");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ORPHAN_PAGE");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "PAGE_NOT_IN_SITEMAP");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_CHAIN");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ROBOTS_NOINDEX");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SCHEMA_MISSING");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SERVER_ERROR_5XX");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SITEMAP_MISSING");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "TOO_DEEP");

            migrationBuilder.InsertData(
                table: "rules",
                columns: new[] { "code", "category", "description_tr", "doc_url", "how_to_fix_tr", "is_active", "severity", "title_tr", "weight" },
                values: new object[,]
                {
                    { "HREFLANG_INVALID", "i18n", "hreflang deger(ler)i gecersiz veya karsilikli degil.", null, "Dil-bolge kodlarini duzelt ve karsilikli hreflang tanimla.", true, "medium", "Gecersiz hreflang", 4 },
                    { "HTTP_STATUS", "indexability", "Sayfa 4xx/5xx durum kodu donuyor ve dizine eklenemez.", null, "Sunucu/yonlendirme yapilandirmasini duzelt, kalici 200 don.", true, "critical", "Sayfa 2xx donmuyor", 10 },
                    { "IMAGE_ALT_MISSING", "images", "Bir veya daha fazla <img> alt niteligi tasimiyor.", null, "Anlamli gorsellere aciklayici alt metni ekle.", true, "low", "Alt metni eksik gorseller", 3 },
                    { "META_DESCRIPTION_LENGTH", "meta", "Meta description 70 karakterden kisa veya 160 karakterden uzun.", null, "Uzunlugu 70-160 karakter araligina cek.", true, "low", "Meta description uzunlugu ideal degil", 3 },
                    { "META_DESCRIPTION_MISSING", "meta", "Sayfada meta description yok; SERP snippet'i kontrolsuz.", null, "70-160 karakter, tiklama tesvik eden bir meta description yaz.", true, "high", "Meta description yok", 6 },
                    { "META_TITLE_LENGTH", "meta", "Title 30 karakterden kisa veya 60 karakterden uzun.", null, "Title'i 30-60 karakter araligina getir.", true, "medium", "Title uzunlugu ideal degil", 5 },
                    { "NOINDEX_DETECTED", "indexability", "robots meta veya X-Robots-Tag noindex iceriyor.", null, "Dizine girmesi gereken sayfalarda noindex'i kaldir.", true, "critical", "noindex etiketi", 9 },
                    { "POOR_LCP", "performance", "Largest Contentful Paint 2.5 sn'nin uzerinde.", null, "Kritik gorselleri onceliklendir, render-blocking kaynaklari azalt.", true, "high", "Kotu LCP", 7 },
                    { "SLOW_TTFB", "performance", "Ilk bayt suresi 800 ms'nin uzerinde.", null, "Sunucu yanit suresini, cache ve CDN kullanimini iyilestir.", true, "medium", "Yavas TTFB", 5 },
                    { "STRUCTURED_DATA_MISSING", "structured_data", "Sayfada schema.org isaretlemesi bulunamadi.", null, "Uygun schema tipini (Article, Product, FAQ...) JSON-LD olarak ekle.", true, "low", "Yapisal veri yok", 3 }
                });
        }
    }
}
