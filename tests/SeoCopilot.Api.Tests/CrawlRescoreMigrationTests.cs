using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Persistence;
using SeoCopilot.Rules;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Yukseltme yolu: eski (toplamsal) formulle skorlanmis taramalar yeni surumde yeniden
/// hesaplanmali, yoksa <c>/compare</c> eski ve yeni taramayi karsilastirinca sahte bir
/// sicrama gosterir. Temiz veritabaninda kosan diger testler bu durumu hic gormez —
/// satirlar eski surume yazilip migration zinciri uzerinden gecirilir.
///
/// Migration'in SQL'i formulu dondurur; buradaki testler o SQL'in bugunku ScoreCalculator
/// ile ayni sonucu verdigini dogrular.
/// </summary>
public class CrawlRescoreMigrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string BeforeRescore = "20260914193554_AddPageImageUrls";

    /// <summary>Sayfa seviyesi kural — rules seed'inde kategorisi meta.</summary>
    private const string PageRule = "META_TITLE_MISSING";

    /// <summary>Crawl seviyesi kurallar — kategorileri indexability ve links.</summary>
    private const string SitemapRule = "SITEMAP_MISSING";
    private const string OrphanRule = "ORPHAN_PAGE";

    private const int PagesCrawled = 10;

    [Fact]
    public async Task Crawls_scored_with_the_old_formula_are_rescored()
    {
        await using var db = NewDatabase();
        var migrator = db.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync(BeforeRescore);

            var crawlId = Guid.NewGuid();
            await SeedLegacyCrawlAsync(db, crawlId, oldOverallScore: "12.34");

            // 2 sayfa seviyesi High (meta), 1 crawl seviyesi Medium (indexability),
            // ayni koddan 3 crawl seviyesi Medium (links) — kod basina tek ceza sayilmali.
            await SeedIssuesAsync(db, crawlId, PageRule, "high", 2);
            await SeedIssuesAsync(db, crawlId, SitemapRule, "medium", 1);
            await SeedIssuesAsync(db, crawlId, OrphanRule, "medium", 3);

            await migrator.MigrateAsync();

            var crawl = await db.Crawls.AsNoTracking().SingleAsync(c => c.Id == crawlId);

            var expected = ScoreCalculator.CalculateCategoryScores(
                pageViolations:
                [
                    V(PageRule, RuleCategory.Meta, Severity.High),
                    V(PageRule, RuleCategory.Meta, Severity.High)
                ],
                crawlLevelViolations:
                [
                    V(SitemapRule, RuleCategory.Indexability, Severity.Medium),
                    V(OrphanRule, RuleCategory.Links, Severity.Medium),
                    V(OrphanRule, RuleCategory.Links, Severity.Medium),
                    V(OrphanRule, RuleCategory.Links, Severity.Medium)
                ],
                pageCount: PagesCrawled);

            AssertSameScores(expected, crawl.CategoryScores);
            Assert.Equal(ScoreCalculator.CalculateOverall(expected), crawl.OverallScore!.Value);

            // Formulun kendisi de dogru olmali — SQL ile C# ayni sekilde bozulmus olmasin.
            Assert.Equal(97m, crawl.CategoryScores["meta"]);          // 100 - 2*15/10
            Assert.Equal(92m, crawl.CategoryScores["indexability"]);  // 100 - 8
            Assert.Equal(92m, crawl.CategoryScores["links"]);         // 3 bulgu, tek kod → 100 - 8
            Assert.Equal(100m, crawl.CategoryScores["images"]);       // ihlali yok
            Assert.Equal(84.67m, crawl.OverallScore!.Value);          // agirlikli eksik 3.26 → egri
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task Rescored_crawl_carries_the_new_scoring_snapshot()
    {
        await using var db = NewDatabase();
        var migrator = db.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync(BeforeRescore);

            var crawlId = Guid.NewGuid();
            await SeedLegacyCrawlAsync(db, crawlId, oldOverallScore: "12.34");
            await SeedIssuesAsync(db, crawlId, PageRule, "high", 1);

            await migrator.MigrateAsync();

            var crawl = await db.Crawls.AsNoTracking().SingleAsync(c => c.Id == crawlId);
            var expected = ScoreCalculator.Snapshot();

            Assert.Equal(expected.Count, crawl.ScoringSnapshot.Count);
            foreach (var (key, value) in expected)
                Assert.Equal(value, crawl.ScoringSnapshot[key]);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task Crawl_without_issues_becomes_a_perfect_score()
    {
        await using var db = NewDatabase();
        var migrator = db.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync(BeforeRescore);

            var crawlId = Guid.NewGuid();
            await SeedLegacyCrawlAsync(db, crawlId, oldOverallScore: "40.00");

            await migrator.MigrateAsync();

            var crawl = await db.Crawls.AsNoTracking().SingleAsync(c => c.Id == crawlId);

            Assert.Equal(100m, crawl.OverallScore!.Value);
            Assert.Equal(ScoreCalculator.CategoryWeights.Count, crawl.CategoryScores.Count);
            Assert.All(crawl.CategoryScores.Values, score => Assert.Equal(100m, score));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task Unscored_crawl_is_left_alone()
    {
        await using var db = NewDatabase();
        var migrator = db.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync(BeforeRescore);

            // Basarisiz tarama: skor hic yazilmadi. Migration bu satira dokunmamali.
            var crawlId = Guid.NewGuid();
            await SeedLegacyCrawlAsync(db, crawlId, oldOverallScore: null, pagesCrawled: 0);

            await migrator.MigrateAsync();

            var crawl = await db.Crawls.AsNoTracking().SingleAsync(c => c.Id == crawlId);

            Assert.Null(crawl.OverallScore);
            Assert.Equal(99.8m, Assert.Single(crawl.CategoryScores).Value);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static void AssertSameScores(
        Dictionary<string, decimal> expected, Dictionary<string, decimal> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var (category, score) in expected)
            Assert.Equal(score, actual[category]);
    }

    private static RuleViolation V(string code, RuleCategory category, Severity severity) =>
        new(code, category, severity, 1, "msg");

    private SeoCopilotDbContext NewDatabase()
    {
        var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = $"legacy_{Guid.NewGuid():N}"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<SeoCopilotDbContext>()
            .UseNpgsql(connection, npg => npg.MigrationsAssembly(typeof(SeoCopilotDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new SeoCopilotDbContext(options);
    }

    /// <summary>
    /// Eski formulle skorlanmis bir tarama satiri. Yabanci anahtar tetikleyicileri kapatilir:
    /// yalniz crawl ve issue satirlari onemli, site/tenant zinciri kurulmaz.
    /// Skoru olmayan tarama icin <paramref name="oldOverallScore"/> null verilir.
    /// </summary>
    private static Task SeedLegacyCrawlAsync(
        SeoCopilotDbContext db, Guid crawlId, string? oldOverallScore, int pagesCrawled = PagesCrawled) =>
        db.Database.ExecuteSqlRawAsync(
            """
            SET session_replication_role = replica;
            INSERT INTO crawls (id, site_id, status, trigger, pages_discovered, pages_crawled,
                overall_score, category_scores, issue_counts, scoring_snapshot, created_at)
            VALUES ({0}, gen_random_uuid(), 'completed', 'manual', {1}, {1},
                CAST(NULLIF({2}, '') AS numeric), '{{"meta": 99.8}}'::jsonb, '{{}}'::jsonb,
                '{{"critical": 25, "high": 15, "medium": 8, "low": 3, "info": 0}}'::jsonb, now());
            SET session_replication_role = origin;
            """,
            crawlId, pagesCrawled, oldOverallScore ?? string.Empty);

    private static Task SeedIssuesAsync(
        SeoCopilotDbContext db, Guid crawlId, string ruleCode, string severity, int count) =>
        db.Database.ExecuteSqlRawAsync(
            """
            SET session_replication_role = replica;
            INSERT INTO issues (crawl_id, site_id, rule_code, severity, weight, evidence, status, created_at)
            SELECT {0}, gen_random_uuid(), {1}, {2}, 1, '{{}}'::jsonb, 'open', now()
            FROM generate_series(1, {3});
            SET session_replication_role = origin;
            """,
            crawlId, ruleCode, severity, count);
}
