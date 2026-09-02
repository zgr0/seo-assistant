using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Services;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Json;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Persistence;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Tarama sonrasi uclar: iptal, filtreli sayfa/bulgu listesi, sayfa detayi, kiyaslama ve
/// bulgu gormezden gelme akisi. Tarama Hangfire'siz, dogrudan orchestrator ile kosulur.
/// </summary>
public class CrawlDetailApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Cancel_marks_a_queued_crawl_cancelled_and_is_not_repeatable()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "cancel-crawl@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Iptal", "https://iptal.example");
        var crawlId = (await SiteManagementApiTests.SeedCrawlsAsync(factory, site.Id, 1))[0];

        var first = await client.PostAsJsonAsync($"/api/crawls/{crawlId}/cancel", new { });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var summary = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", summary.GetProperty("status").GetString());

        // Bitmis tarama tekrar iptal edilemez.
        var second = await client.PostAsJsonAsync($"/api/crawls/{crawlId}/cancel", new { });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Cancelled_crawl_is_skipped_by_the_worker()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "cancel-worker@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Iptal2", webSite.BaseUrl);
        var crawlId = (await SiteManagementApiTests.SeedCrawlsAsync(factory, site.Id, 1))[0];

        await client.PostAsJsonAsync($"/api/crawls/{crawlId}/cancel", new { });

        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<CrawlOrchestrator>().RunAsync(crawlId);

        var summary = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{crawlId}");
        Assert.Equal("Cancelled", summary.GetProperty("status").GetString());
        Assert.Equal(0, summary.GetProperty("pagesCrawled").GetInt32());
    }

    [Fact]
    public async Task Pages_can_be_filtered_by_status_and_url()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, crawlId) = await CrawlAsync(factory, webSite, "pages-filter@example.com");

        // Kirik sayfa ve kirik PDF varligi — ikisi de 404
        var broken = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{crawlId}/pages?statusCode=404");
        Assert.Equal(2, broken.GetProperty("total").GetInt32());
        var brokenUrls = broken.GetProperty("items")
            .EnumerateArray()
            .Select(i => i.GetProperty("url").GetString()!)
            .ToList();
        Assert.Contains(brokenUrls, u => u.EndsWith("/kirik"));
        Assert.Contains(brokenUrls, u => u.EndsWith("/dosyalar/eksik.pdf"));

        var byUrl = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{crawlId}/pages?url=sitemap-only");
        Assert.Equal(1, byUrl.GetProperty("total").GetInt32());

        var depthZero = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{crawlId}/pages?depth=0");
        Assert.Equal(2, depthZero.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Issues_are_paged_and_filterable_by_rule_code_and_severity()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, crawlId) = await CrawlAsync(factory, webSite, "issues-filter@example.com");

        var all = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{crawlId}/issues?size=5");
        Assert.True(all.GetProperty("total").GetInt32() > 5);
        Assert.Equal(5, all.GetProperty("items").GetArrayLength());

        var byRule = await client.GetFromJsonAsync<JsonElement>(
            $"/api/crawls/{crawlId}/issues?ruleCode=DUPLICATE_CONTENT");
        Assert.Equal(1, byRule.GetProperty("total").GetInt32());
        Assert.Equal("Content", byRule.GetProperty("items")[0].GetProperty("category").GetString());

        var critical = await client.GetFromJsonAsync<JsonElement>(
            $"/api/crawls/{crawlId}/issues?severity=critical&size=200");
        Assert.All(
            critical.GetProperty("items").EnumerateArray(),
            i => Assert.Equal("Critical", i.GetProperty("severity").GetString()));

        // Kategori snake_case da kabul edilir.
        var structured = await client.GetFromJsonAsync<JsonElement>(
            $"/api/crawls/{crawlId}/issues?category=structured_data&size=200");
        Assert.True(structured.GetProperty("total").GetInt32() > 0);
    }

    [Fact]
    public async Task Issues_are_sorted_by_severity_rank_not_alphabetically()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, crawlId) = await CrawlAsync(factory, webSite, "issues-order@example.com");

        var body = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{crawlId}/issues?size=200");
        var order = body.GetProperty("items").EnumerateArray()
            .Select(i => Rank(i.GetProperty("severity").GetString()!))
            .ToList();

        // severity kolonu metin oldugu icin alfabetik siralama 'medium'u basa alirdi.
        Assert.Equal(4, order[0]);
        Assert.Equal(order, order.OrderByDescending(r => r));

        var high = await client.GetFromJsonAsync<JsonElement>(
            $"/api/crawls/{crawlId}/issues?minSeverity=high&size=200");
        Assert.True(high.GetProperty("total").GetInt32() > 0);
        Assert.All(
            high.GetProperty("items").EnumerateArray(),
            i => Assert.True(Rank(i.GetProperty("severity").GetString()!) >= 3));

        static int Rank(string severity) => severity switch
        {
            "Critical" => 4,
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 0,
        };
    }

    [Fact]
    public async Task Invalid_severity_filter_is_400()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, crawlId) = await CrawlAsync(factory, webSite, "issues-badfilter@example.com");

        var res = await client.GetAsync($"/api/crawls/{crawlId}/issues?severity=felaket");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Page_detail_returns_main_text_and_its_issues()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, crawlId) = await CrawlAsync(factory, webSite, "page-detail@example.com");

        var pages = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{crawlId}/pages?depth=0&size=5");
        var pageId = pages.GetProperty("items")[0].GetProperty("id").GetGuid();

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/pages/{pageId}");

        Assert.Equal(crawlId, detail.GetProperty("crawlId").GetGuid());
        Assert.Equal(pageId, detail.GetProperty("page").GetProperty("id").GetGuid());
        Assert.False(string.IsNullOrWhiteSpace(detail.GetProperty("mainText").GetString()));
        Assert.True(detail.GetProperty("issues").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Page_of_another_tenant_is_404()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, crawlId) = await CrawlAsync(factory, webSite, "page-guard-owner@example.com");
        var pages = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{crawlId}/pages?size=1");
        var pageId = pages.GetProperty("items")[0].GetProperty("id").GetGuid();

        var (stranger, _) = await TestAuth.RegisterAsync(factory, "page-guard-stranger@example.com");

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/pages/{pageId}")).StatusCode);
    }

    [Fact]
    public async Task Compare_of_two_identical_crawls_reports_no_change()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, first) = await CrawlAsync(factory, webSite, "compare@example.com");
        var second = await RunCrawlAsync(factory, await SiteIdOfAsync(factory, first));

        var body = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{second}/compare/{first}");

        Assert.Equal(0m, body.GetProperty("scoreDelta").GetDecimal());
        Assert.Equal(
            body.GetProperty("previousPagesCrawled").GetInt32(),
            body.GetProperty("currentPagesCrawled").GetInt32());
        Assert.True(body.GetProperty("unchangedIssueCount").GetInt32() > 0);

        // Onem dagilimi birebir ayni; hangi sayfaya yazildigi crawl seviyesi kurallarda
        // degisebildigi icin new/resolved listeleri bos olmak zorunda degil.
        Assert.All(
            body.GetProperty("issueCountDelta").EnumerateObject(),
            p => Assert.Equal(0, p.Value.GetInt32()));
    }

    [Fact]
    public async Task Ignore_and_reopen_round_trip()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, crawlId) = await CrawlAsync(factory, webSite, "issue-ignore@example.com");

        // Birden fazla sayfada tetiklenen bir kural sec — applyToSite'i olcebilmek icin.
        var all = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{crawlId}/issues?size=200");
        var group = all.GetProperty("items").EnumerateArray()
            .Where(i => i.GetProperty("pageId").ValueKind != JsonValueKind.Null)
            .GroupBy(i => i.GetProperty("ruleCode").GetString()!)
            .First(g => g.Count() > 1);

        var ruleCode = group.Key;
        var issueId = group.First().GetProperty("id").GetInt64();

        var ignored = await client.PostAsJsonAsync(
            $"/api/issues/{issueId}/ignore", new { reason = "Bilincli tercih", applyToSite = true });
        Assert.Equal(HttpStatusCode.OK, ignored.StatusCode);
        Assert.Equal("Ignored", (await ignored.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString());

        // applyToSite: ayni kuraldaki tum acik bulgular kapandi
        var open = await client.GetFromJsonAsync<JsonElement>(
            $"/api/crawls/{crawlId}/issues?ruleCode={ruleCode}&status=open&size=200");
        Assert.Equal(0, open.GetProperty("total").GetInt32());

        var reopened = await client.PostAsJsonAsync($"/api/issues/{issueId}/reopen", new { });
        Assert.Equal("Open", (await reopened.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString());
    }

    [Fact]
    public async Task Ignore_without_a_reason_is_400()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, crawlId) = await CrawlAsync(factory, webSite, "issue-noreason@example.com");
        var issues = await client.GetFromJsonAsync<JsonElement>($"/api/crawls/{crawlId}/issues?size=1");
        var issueId = issues.GetProperty("items")[0].GetProperty("id").GetInt64();

        var res = await client.PostAsJsonAsync($"/api/issues/{issueId}/ignore", new { reason = "  " });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // --- yardimcilar ---

    private static async Task<(HttpClient Client, Guid CrawlId)> CrawlAsync(
        WebApplicationFactory<Program> factory, TestWebSite webSite, string email)
    {
        var (client, _) = await TestAuth.RegisterAsync(factory, email);
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Tarama", webSite.BaseUrl);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
            var entity = await db.Sites.FirstAsync(s => s.Id == site.Id);
            entity.CrawlSettings = new CrawlSettings { DelayMs = 0 };
            await db.SaveChangesAsync();
        }

        return (client, await RunCrawlAsync(factory, site.Id));
    }

    private static async Task<Guid> RunCrawlAsync(WebApplicationFactory<Program> factory, Guid siteId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();

        var crawl = new Crawl { SiteId = siteId, Status = CrawlStatus.Queued, Trigger = CrawlTrigger.Manual };
        db.Crawls.Add(crawl);
        await db.SaveChangesAsync();

        await scope.ServiceProvider.GetRequiredService<CrawlOrchestrator>().RunAsync(crawl.Id);
        return crawl.Id;
    }

    private static async Task<Guid> SiteIdOfAsync(WebApplicationFactory<Program> factory, Guid crawlId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
        return await db.Crawls.Where(c => c.Id == crawlId).Select(c => c.SiteId).FirstAsync();
    }
}
