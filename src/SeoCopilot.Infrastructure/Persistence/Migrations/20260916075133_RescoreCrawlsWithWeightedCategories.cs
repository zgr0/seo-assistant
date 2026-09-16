using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Gecmis taramalari yeni skor formuluyle yeniden hesaplar.
    ///
    /// Eski model kategori skorunu bagimsiz (her biri 100'den baslayan) hesaplar, genel skoru
    /// ise tum cezalari toplayarak buluyordu; ustelik crawl seviyesi bulgunun cezasi hem
    /// kategoriye (sayfa sayisina bolunmus) hem genele (tam) yaziliyordu. Sonuc: kategori
    /// barlari 95+ gorunurken genel skor 0-20 bandina yapisiyordu.
    ///
    /// Yeni model: bulgu -> kategori skoru -> genel skor. Her bulgu bir kez sayilir, genel skor
    /// kategorilerin agirlikli eksiginden doygunluk egrisiyle turer. Bu migration olmadan
    /// <c>/compare</c> eski ve yeni taramayi karsilastirdiginda +40..+70 gibi sahte bir sicrama
    /// gosterirdi.
    ///
    /// Formul buraya sabitlenmistir — ScoreCalculator ileride degisse de bu migration'in
    /// urettigi degerler donmus kalir; yeni bir formul yeni bir migration ister.
    ///
    /// Bulgu durumu (open/ignored/fixed) dikkate alinmaz: skor tarama anindaki gercegi anlatir,
    /// o anda her bulgu Open yazilmisti. Yoksayma okuma tarafinda filtrelenir, skoru degistirmez.
    /// </summary>
    public partial class RescoreCrawlsWithWeightedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH crawl_level(code) AS (
                    -- CrawlRules.Evaluate'in urettigi kod kumesi: tek sayfaya bakarak karar
                    -- verilemeyen kurallar. Cezalari sayfa sayisina bolunmez.
                    VALUES ('DUPLICATE_CONTENT'), ('META_TITLE_DUPLICATE'), ('META_DESC_DUPLICATE'),
                           ('BROKEN_INTERNAL_LINK'), ('ORPHAN_PAGE'), ('TOO_DEEP'),
                           ('SITEMAP_MISSING'), ('PAGE_NOT_IN_SITEMAP'), ('BLOCKED_BY_ROBOTS_TXT'),
                           ('LCP_POOR'), ('CLS_POOR'), ('INP_POOR')
                ),
                weights(category, weight) AS (
                    VALUES ('indexability', 20), ('meta', 18), ('content', 18), ('links', 14),
                           ('performance', 12), ('images', 8), ('structured_data', 6), ('i18n', 4)
                ),
                target AS (
                    -- Skorlanmis taramalar. Basarisiz/kuyruktaki taramalarin skoru null kalir.
                    SELECT id, pages_crawled
                    FROM crawls
                    WHERE overall_score IS NOT NULL AND pages_crawled > 0
                ),
                scored AS (
                    SELECT
                        i.crawl_id,
                        r.category,
                        i.rule_code,
                        (CASE i.severity
                            WHEN 'critical' THEN 25
                            WHEN 'high' THEN 15
                            WHEN 'medium' THEN 8
                            WHEN 'low' THEN 3
                            ELSE 0
                        END)::numeric AS penalty,
                        i.rule_code IN (SELECT code FROM crawl_level) AS is_crawl_level
                    FROM issues i
                    JOIN rules r ON r.code = i.rule_code
                    WHERE i.crawl_id IN (SELECT id FROM target)
                ),
                page_penalty AS (
                    SELECT crawl_id, category, SUM(penalty) AS penalty
                    FROM scored
                    WHERE NOT is_crawl_level
                    GROUP BY crawl_id, category
                ),
                crawl_penalty AS (
                    -- Kural kodu basina bir kez: 300 oksuz sayfa tek ORPHAN_PAGE cezasi eder.
                    SELECT crawl_id, category, SUM(penalty) AS penalty
                    FROM (
                        SELECT crawl_id, category, rule_code, MAX(penalty) AS penalty
                        FROM scored
                        WHERE is_crawl_level
                        GROUP BY crawl_id, category, rule_code
                    ) per_code
                    GROUP BY crawl_id, category
                ),
                category_score AS (
                    -- CROSS JOIN: ihlali olmayan kategori de tabloya 100 olarak girer.
                    SELECT
                        t.id AS crawl_id,
                        w.category,
                        w.weight,
                        GREATEST(0, LEAST(100, round(
                            100
                            - COALESCE(pp.penalty, 0) / t.pages_crawled
                            - COALESCE(cp.penalty, 0), 2))) AS score
                    FROM target t
                    CROSS JOIN weights w
                    LEFT JOIN page_penalty pp ON pp.crawl_id = t.id AND pp.category = w.category
                    LEFT JOIN crawl_penalty cp ON cp.crawl_id = t.id AND cp.category = w.category
                ),
                rolled AS (
                    SELECT
                        crawl_id,
                        jsonb_object_agg(category, score) AS category_scores,
                        SUM(weight * (100 - score)) / SUM(weight) AS deficit
                    FROM category_score
                    GROUP BY crawl_id
                )
                UPDATE crawls c
                SET category_scores = rolled.category_scores,
                    overall_score = CASE
                        WHEN rolled.deficit <= 0 THEN 100
                        -- Doygunluk egrisi: eksik 18'e esitken skor tam 50.
                        ELSE round(100 - (100 * rolled.deficit / (rolled.deficit + 18)), 2)
                    END,
                    scoring_snapshot = '{
                        "critical": 25, "high": 15, "medium": 8, "low": 3, "info": 0,
                        "category_weight_indexability": 20,
                        "category_weight_meta": 18,
                        "category_weight_content": 18,
                        "category_weight_links": 14,
                        "category_weight_performance": 12,
                        "category_weight_images": 8,
                        "category_weight_structured_data": 6,
                        "category_weight_i18n": 4,
                        "overall_half_point": 18
                    }'::jsonb
                FROM rolled
                WHERE c.id = rolled.crawl_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eski skorlar hicbir yerde saklanmadigi icin geri alinamaz. Bulgular (issues)
            // duruyor; eski formul gerekirse oradan yeniden hesaplanabilir.
        }
    }
}
