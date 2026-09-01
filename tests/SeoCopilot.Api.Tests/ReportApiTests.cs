using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Api.Tests;

/// <summary>Rapor istegi, uretimi ve indirilmesi. Uretim Hangfire'siz, dogrudan tetiklenir.</summary>
public class ReportApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Report_request_without_a_crawl_is_400()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "report-nocrawl@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Rapor", "https://rapor.example");

        var res = await client.PostAsJsonAsync($"/api/sites/{site.Id}/reports", new { });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Report_is_queued_then_downloadable_once_generated()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "report-flow@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Rapor", "https://rapor-flow.example");
        var crawlId = (await SiteManagementApiTests.SeedCrawlsAsync(factory, site.Id, 1))[0];

        var created = await client.PostAsJsonAsync($"/api/sites/{site.Id}/reports", new { crawlId });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var report = await created.Content.ReadFromJsonAsync<JsonElement>();
        var reportId = report.GetProperty("id").GetGuid();
        Assert.Equal(crawlId, report.GetProperty("crawlId").GetGuid());

        // Hazir olmadan indirilemez.
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.GetAsync($"/api/reports/{reportId}/download")).StatusCode);

        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ReportService>().RunAsync(reportId);

        var status = await client.GetFromJsonAsync<JsonElement>($"/api/reports/{reportId}");
        Assert.Equal("Done", status.GetProperty("status").GetString());
        Assert.Equal($"/api/reports/{reportId}/download", status.GetProperty("downloadUrl").GetString());

        var download = await client.GetAsync($"/api/reports/{reportId}/download");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("text/html", download.Content.Headers.ContentType!.MediaType);
        Assert.Contains("SEO raporu", await download.Content.ReadAsStringAsync());

        var list = await client.GetFromJsonAsync<JsonElement>($"/api/sites/{site.Id}/reports");
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task Report_of_another_tenant_is_404()
    {
        await using var factory = fixture.CreateFactory();
        var (owner, _) = await TestAuth.RegisterAsync(factory, "report-owner@example.com");
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "report-stranger@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(owner, "Rapor", "https://rapor-guard.example");
        var crawlId = (await SiteManagementApiTests.SeedCrawlsAsync(factory, site.Id, 1))[0];

        var created = await owner.PostAsJsonAsync($"/api/sites/{site.Id}/reports", new { crawlId });
        var reportId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/reports/{reportId}")).StatusCode);
    }
}
