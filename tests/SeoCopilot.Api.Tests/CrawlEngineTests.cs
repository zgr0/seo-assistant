using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Services;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Json;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Persistence;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Tarama motorunu gercek HTTP + gercek Postgres uzerinde ucdan uca calistirir.
/// Hangfire devre disi birakilmadan, is dogrudan CrawlOrchestrator uzerinden tetiklenir
/// ki sonuc deterministik olsun.
/// </summary>
public class CrawlEngineTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private sealed record CrawlResult(Crawl Crawl, List<Page> Pages, List<PageLink> Links, List<Domain.Entities.Rules.Issue> Issues);

    private async Task<CrawlResult> CrawlAsync(
        WebApplicationFactory<Program> factory,
        TestWebSite webSite,
        string email,
        CrawlSettings? settings = null)
    {
        var (client, _) = await TestAuth.RegisterAsync(factory, email);
        var created = await client.PostAsJsonAsync("/api/sites", new { name = "Test", baseUrl = webSite.BaseUrl });
        created.EnsureSuccessStatusCode();
        var site = (await created.Content.ReadFromJsonAsync<SiteResponse>())!;

        // Dogrulamayi ve ayarlari dogrudan yaz — bu test tarama motorunu olcuyor.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
            var entity = await db.Sites.FirstAsync(s => s.Id == site.Id);
            entity.CrawlSettings = settings ?? new CrawlSettings { DelayMs = 0 };
            await db.SaveChangesAsync();
        }

        Guid crawlId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
            var crawl = new Crawl { SiteId = site.Id, Status = CrawlStatus.Queued, Trigger = CrawlTrigger.Manual };
            db.Crawls.Add(crawl);
            await db.SaveChangesAsync();
            crawlId = crawl.Id;

            await scope.ServiceProvider.GetRequiredService<CrawlOrchestrator>().RunAsync(crawlId);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
            return new CrawlResult(
                await db.Crawls.AsNoTracking().FirstAsync(c => c.Id == crawlId),
                await db.Pages.AsNoTracking().Where(p => p.CrawlId == crawlId).ToListAsync(),
                await db.PageLinks.AsNoTracking().Where(l => l.CrawlId == crawlId).ToListAsync(),
                await db.Issues.AsNoTracking().Where(i => i.CrawlId == crawlId).ToListAsync());
        }
    }

    /// <summary>Ikili varlik satirlari haric, taranan HTML sayfalarinin yollari.</summary>
    private static List<string> HtmlPaths(CrawlResult result) =>
        [.. result.Pages
            .Where(p => !UrlNormalizer.IsLikelyAsset(new Uri(p.Url)))
            .Select(p => new Uri(p.Url).AbsolutePath)
            .OrderBy(p => p)];

    private static List<Page> AssetRows(CrawlResult result) =>
        [.. result.Pages.Where(p => UrlNormalizer.IsLikelyAsset(new Uri(p.Url)))];

    [Fact]
    public async Task Crawls_every_reachable_page_and_honours_robots()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-basic@example.com");
        var paths = HtmlPaths(result);

        Assert.Equal(CrawlStatus.Completed, result.Crawl.Status);
        Assert.Equal(["/", "/a", "/b", "/c", "/eski-adres", "/kirik", "/sitemap-only"], paths);

        // robots.txt "/gizli" yolunu kapatiyor
        Assert.DoesNotContain(result.Pages, p => p.Url.Contains("/gizli"));

        Assert.Equal(7, result.Crawl.PagesCrawled);
        Assert.Equal(7, result.Crawl.PagesDiscovered);
    }

    [Fact]
    public async Task Depth_is_recorded_and_sitemap_only_pages_are_seeds()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-depth@example.com");
        var byPath = result.Pages.ToDictionary(p => new Uri(p.Url).AbsolutePath, p => p);

        Assert.Equal(0, byPath["/"].Depth);
        Assert.Equal(0, byPath["/sitemap-only"].Depth); // sitemap'ten geldigi icin tohum
        Assert.Equal(1, byPath["/a"].Depth);
        Assert.Equal(2, byPath["/c"].Depth);
    }

    [Fact]
    public async Task Redirected_url_is_recorded_but_not_judged_as_its_own_page()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-redirect@example.com");
        var byPath = result.Pages.ToDictionary(p => new Uri(p.Url).AbsolutePath, p => p);

        // Satir kalir: kirik link cozumu ve REDIRECT_CHAIN icin gerekli.
        var redirected = byPath["/eski-adres"];
        Assert.Equal(200, redirected.StatusCode);
        Assert.EndsWith("/a", redirected.RedirectTo);

        // Govde "/a"ya ait — icerik bulgulari yalniz hedefte uretilir.
        var contentCodes = result.Issues
            .Where(i => i.PageId == redirected.Id)
            .Select(i => i.RuleCode)
            .Where(code => code is not ("BROKEN_PAGE_4XX" or "SERVER_ERROR_5XX" or "REDIRECT_CHAIN"))
            .ToList();
        Assert.Empty(contentCodes);

        // Yonlendirme kaynagi kopya tespitine girmez; "/a" ve "/b" gercek kopyalar.
        var duplicates = result.Issues.Where(i => i.RuleCode == "DUPLICATE_CONTENT").ToList();
        Assert.All(duplicates, i =>
            Assert.DoesNotContain("/eski-adres", string.Join(" ", i.Evidence.SampleUrls)));

        // Hedef normal sekilde taranmis olmali.
        Assert.Equal(200, byPath["/a"].StatusCode);
        Assert.Null(byPath["/a"].RedirectTo);
    }

    [Fact]
    public async Task Depth_does_not_drift_with_higher_concurrency()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        // Paralel getiriciler bir URL'i once hangi ebeveynin kesfettigini yanit surelerine
        // birakir. Derinlik link grafigindan hesaplandigi icin sonuc bundan etkilenmemeli.
        var result = await CrawlAsync(factory, webSite, "engine-depth-concurrent@example.com",
            new CrawlSettings { DelayMs = 0, Concurrency = 8 });
        var byPath = result.Pages.ToDictionary(p => new Uri(p.Url).AbsolutePath, p => p);

        Assert.Equal(0, byPath["/"].Depth);
        Assert.Equal(0, byPath["/sitemap-only"].Depth);
        Assert.Equal(1, byPath["/a"].Depth);
        Assert.Equal(1, byPath["/b"].Depth);
        Assert.Equal(2, byPath["/c"].Depth);
    }

    [Fact]
    public async Task Page_columns_are_populated()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-columns@example.com");
        var home = result.Pages.Single(p => new Uri(p.Url).AbsolutePath == "/");

        Assert.Equal(200, home.StatusCode);
        Assert.Contains("text/html", home.ContentType);
        Assert.Equal("tr", home.Lang);
        Assert.EndsWith("/", home.CanonicalUrl);
        Assert.Equal(["Ana sayfa"], home.H1Texts);
        Assert.Equal(1, home.H2Count);
        Assert.Equal(1, home.ImagesTotal);
        Assert.Equal(0, home.ImagesNoAlt);
        Assert.Contains("WebSite", home.SchemaTypes);
        Assert.NotNull(home.ContentHash);
        Assert.NotNull(home.MainText);
        Assert.True(home.WordCount > 0);
        Assert.True(home.HtmlSizeBytes > 0);
        Assert.NotNull(home.ResponseTimeMs);

        // dis link + nofollow ayrimi
        Assert.Equal(1, home.OutlinkExternal);
        Assert.Equal(7, home.OutlinkInternal); // /a /b /eski-adres /kirik /gizli/x + iki pdf

        var noIndex = result.Pages.Single(p => new Uri(p.Url).AbsolutePath == "/c");
        Assert.Contains("noindex", noIndex.RobotsMeta);
    }

    [Fact]
    public async Task Link_graph_is_written_and_inlinks_counted()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-links@example.com");
        var byPath = result.Pages.ToDictionary(p => new Uri(p.Url).AbsolutePath, p => p);

        Assert.NotEmpty(result.Links);
        Assert.Contains(result.Links, l => l.ToPageId is not null);

        var external = Assert.Single(result.Links, l => !l.IsInternal);
        Assert.True(external.IsNofollow);
        Assert.Null(external.ToPageId);
        Assert.Equal("dis site", external.AnchorText);

        // /c'ye hem /a hem /b link veriyor
        Assert.Equal(2, byPath["/c"].InlinkCount);
        Assert.Equal(1, byPath["/a"].InlinkCount);
        Assert.Equal(0, byPath["/"].InlinkCount);

        // robots ile engellenen sayfa taranmadi ama link kaydi duruyor
        var blocked = Assert.Single(result.Links, l => l.ToUrl.EndsWith("/gizli/x"));
        Assert.Null(blocked.ToPageId);
    }

    [Fact]
    public async Task Every_issue_rule_code_exists_in_the_rules_table()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-rulecodes@example.com");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
        var known = await db.Rules.AsNoTracking().Select(r => r.Code).ToListAsync();

        Assert.NotEmpty(result.Issues);
        Assert.All(result.Issues, i => Assert.Contains(i.RuleCode, known));
        Assert.All(result.Issues, i => Assert.True(i.Weight > 0));
    }

    [Fact]
    public async Task Crawl_level_rules_catch_duplicates_and_broken_links()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-crawlrules@example.com");
        var codes = result.Issues.Select(i => i.RuleCode).ToHashSet();

        Assert.Contains("DUPLICATE_CONTENT", codes);
        Assert.Contains("BROKEN_INTERNAL_LINK", codes);
        Assert.Contains("ROBOTS_NOINDEX", codes);
        Assert.Contains("BROKEN_PAGE_4XX", codes); // /kirik 404

        // /a ve /b ayni title + ayni aciklama
        Assert.Contains("META_TITLE_DUPLICATE", codes);
        Assert.Contains("META_DESC_DUPLICATE", codes);

        var duplicate = result.Issues.Single(i => i.RuleCode == "DUPLICATE_CONTENT");
        Assert.Equal(2, duplicate.Evidence.SampleUrls.Count);
    }

    [Fact]
    public async Task Robots_blocked_urls_and_sitemap_gaps_are_reported()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-indexability@example.com");
        var byCode = result.Issues.ToLookup(i => i.RuleCode);

        // robots.txt "/gizli" yolunu kapatiyor, ana sayfa oraya link veriyor
        var blocked = Assert.Single(byCode["BLOCKED_BY_ROBOTS_TXT"]);
        Assert.Contains(blocked.Evidence.SampleUrls, u => u.EndsWith("/gizli/x"));

        // sitemap "/" ve "/sitemap-only" iceriyor; /a ve /b disarida
        var notInSitemap = byCode["PAGE_NOT_IN_SITEMAP"]
            .SelectMany(i => i.Evidence.SampleUrls)
            .Select(u => new Uri(u).AbsolutePath)
            .ToHashSet();
        Assert.Contains("/a", notInSitemap);
        Assert.Contains("/b", notInSitemap);
        Assert.DoesNotContain("/sitemap-only", notInSitemap);

        Assert.Empty(byCode["SITEMAP_MISSING"]);

        // /sitemap-only sayfasina hicbir ic link yok
        var orphan = Assert.Single(byCode["ORPHAN_PAGE"]);
        Assert.EndsWith("/sitemap-only", orphan.Evidence.SampleUrls[0]);

        // /b canonical'i /a'yi gosteriyor
        var canonical = Assert.Single(byCode["CANONICAL_POINTS_ELSEWHERE"]);
        Assert.NotNull(canonical.PageId);
    }

    [Fact]
    public async Task Scores_and_snapshot_are_written()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-scores@example.com");

        Assert.NotNull(result.Crawl.OverallScore);
        Assert.InRange(result.Crawl.OverallScore!.Value, 0m, 100m);
        Assert.NotEmpty(result.Crawl.CategoryScores);
        Assert.NotEmpty(result.Crawl.IssueCounts);
        Assert.Equal(25, result.Crawl.ScoringSnapshot["critical"]);
    }

    [Fact]
    public async Task Max_pages_cap_marks_the_crawl_partial()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-partial@example.com",
            new CrawlSettings { MaxPages = 2, DelayMs = 0 });

        Assert.Equal(CrawlStatus.Partial, result.Crawl.Status);
        Assert.Equal(2, result.Crawl.PagesCrawled);
    }

    [Fact]
    public async Task Max_depth_zero_crawls_only_the_seeds()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-depth0@example.com",
            new CrawlSettings { MaxDepth = 0, DelayMs = 0 });

        Assert.Equal(CrawlStatus.Partial, result.Crawl.Status);
        Assert.Equal(2, result.Crawl.PagesCrawled); // "/" + sitemap'teki "/sitemap-only"
    }

    [Fact]
    public async Task Exclude_pattern_skips_matching_urls()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-exclude@example.com",
            new CrawlSettings { DelayMs = 0, ExcludePatterns = ["/kirik$", "/c$"] });

        var paths = result.Pages.Select(p => new Uri(p.Url).AbsolutePath).ToList();
        Assert.DoesNotContain("/kirik", paths);
        Assert.DoesNotContain("/c", paths);
        Assert.Contains("/a", paths);
    }

    [Fact]
    public async Task Binary_assets_are_probed_but_not_counted_as_pages()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-assets@example.com");
        var assets = AssetRows(result);

        Assert.Equal(2, assets.Count);

        var katalog = assets.Single(a => a.Url.EndsWith("/katalog.pdf"));
        Assert.Equal(200, katalog.StatusCode);
        Assert.Contains("application/pdf", katalog.ContentType);
        Assert.Equal(404, assets.Single(a => a.Url.EndsWith("/eksik.pdf")).StatusCode);

        // Govde indirilmedi — sayfa alanlari bos
        Assert.Null(katalog.Title);
        Assert.Equal(0, katalog.WordCount);
        Assert.Null(katalog.ContentHash);

        // Sayfa butcesinden dusmez
        Assert.Equal(7, result.Crawl.PagesCrawled);
        Assert.Equal(7, result.Crawl.PagesDiscovered);
    }

    [Fact]
    public async Task Assets_are_not_evaluated_by_page_rules()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-assetrules@example.com");
        var assetIds = AssetRows(result).Select(a => a.Id).ToHashSet();

        // 200 donen bir PDF "title yok, h1 yok, icerik zayif" diye raporlanmamali.
        Assert.DoesNotContain(result.Issues, i => i.PageId is Guid id && assetIds.Contains(id));
    }

    [Fact]
    public async Task Broken_asset_link_is_reported_as_a_broken_internal_link()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-brokenasset@example.com");

        var broken = result.Issues.Single(i => i.RuleCode == "BROKEN_INTERNAL_LINK");
        Assert.Contains(broken.Evidence.SampleUrls, u => u.EndsWith("/eksik.pdf"));
        Assert.Contains(broken.Evidence.SampleUrls, u => u.EndsWith("/kirik"));
    }

    [Fact]
    public async Task Asset_probing_can_be_switched_off()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-noassets@example.com",
            new CrawlSettings { DelayMs = 0, MaxAssetChecks = 0 });

        Assert.Empty(AssetRows(result));
        Assert.Equal(7, result.Crawl.PagesCrawled);

        // Yoklama kapaliyken hedef durumu bilinmez — kirik varlik linki de bildirilmez.
        var broken = result.Issues.Single(i => i.RuleCode == "BROKEN_INTERNAL_LINK");
        Assert.DoesNotContain(broken.Evidence.SampleUrls, u => u.EndsWith("/eksik.pdf"));
    }

    [Fact]
    public async Task Summary_endpoint_reflects_the_finished_crawl()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();

        var result = await CrawlAsync(factory, webSite, "engine-summary@example.com");

        var (client, _) = await TestAuth.RegisterAsync(factory, "engine-summary-reader@example.com");
        // Baska kiraci ayni crawl'i goremez
        Assert.Equal(
            System.Net.HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/crawls/{result.Crawl.Id}")).StatusCode);
    }
}
