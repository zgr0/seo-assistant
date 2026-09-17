using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Api.Tests;

/// <summary>Rapor istegi, uretimi ve indirilmesi. Uretim Hangfire'siz, dogrudan tetiklenir.</summary>
public class ReportApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    /// <summary>
    /// Tarayici acmadan PDF yolunu dogrulamak icin. Gercek Chromium ciktisi
    /// <see cref="Pdf_report_is_rendered_by_chromium"/> ile denenir.
    /// </summary>
    private sealed class FakePdfRenderer : IPdfRenderer
    {
        public const string Marker = "%PDF-1.4 sahte";

        public byte[]? LastHtml { get; private set; }

        public Task<byte[]> RenderAsync(byte[] html, CancellationToken ct = default)
        {
            LastHtml = html;
            return Task.FromResult(Encoding.UTF8.GetBytes(Marker));
        }
    }

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

        var created = await client.PostAsJsonAsync(
            $"/api/sites/{site.Id}/reports", new { crawlId, format = "html" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var report = await created.Content.ReadFromJsonAsync<JsonElement>();
        var reportId = report.GetProperty("id").GetGuid();
        Assert.Equal(crawlId, report.GetProperty("crawlId").GetGuid());
        Assert.Equal("Html", report.GetProperty("format").GetString());

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
    public async Task Report_without_a_format_is_generated_as_pdf()
    {
        var renderer = new FakePdfRenderer();
        await using var factory = fixture.CreateFactory(services =>
            services.AddSingleton<IPdfRenderer>(renderer));

        var (client, _) = await TestAuth.RegisterAsync(factory, "report-pdf@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Rapor", "https://rapor-pdf.example");
        var crawlId = (await SiteManagementApiTests.SeedCrawlsAsync(factory, site.Id, 1))[0];

        var created = await client.PostAsJsonAsync($"/api/sites/{site.Id}/reports", new { crawlId });
        var reportId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ReportService>().RunAsync(reportId);

        var status = await client.GetFromJsonAsync<JsonElement>($"/api/reports/{reportId}");
        Assert.Equal("Pdf", status.GetProperty("format").GetString());

        var download = await client.GetAsync($"/api/reports/{reportId}/download");
        Assert.Equal("application/pdf", download.Content.Headers.ContentType!.MediaType);
        Assert.EndsWith(".pdf", download.Content.Headers.ContentDisposition!.FileName!.Trim('"'));
        Assert.Equal(FakePdfRenderer.Marker, await download.Content.ReadAsStringAsync());

        // Basilan sey rapor HTML'inin ta kendisi; PDF yolu ayri bir icerik uretmiyor.
        Assert.Contains("SEO raporu", Encoding.UTF8.GetString(renderer.LastHtml!));
    }

    [Fact]
    public async Task Unknown_format_is_400()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "report-badformat@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Rapor", "https://rapor-bad.example");
        var crawlId = (await SiteManagementApiTests.SeedCrawlsAsync(factory, site.Id, 1))[0];

        var res = await client.PostAsJsonAsync(
            $"/api/sites/{site.Id}/reports", new { crawlId, format = "docx" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>
    /// Gercek Chromium ile basar. Playwright tarayicisi her gelistirici makinesinde kurulu
    /// olmadigindan varsayilan kosuda atlanir:
    ///
    ///   $env:SEOCOPILOT_PDF_TESTS = "1"
    ///   dotnet test --filter "FullyQualifiedName~Pdf_report_is_rendered_by_chromium"
    ///
    /// Tarayici kurulumu: <c>tests/.../bin/Debug/net10.0/playwright.ps1 install chromium-headless-shell</c>
    /// LIVE_OUTPUT_DIR tanimliysa uretilen PDF oraya yazilir (goz denetimi icin).
    /// </summary>
    [Fact]
    [Trait("Category", "Live")]
    public async Task Pdf_report_is_rendered_by_chromium()
    {
        if (Environment.GetEnvironmentVariable("SEOCOPILOT_PDF_TESTS") != "1") return;

        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "report-chromium@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Rapor", "https://rapor-chromium.example");
        var crawlId = (await SiteManagementApiTests.SeedCrawlsAsync(factory, site.Id, 1))[0];

        var created = await client.PostAsJsonAsync($"/api/sites/{site.Id}/reports", new { crawlId });
        var reportId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ReportService>().RunAsync(reportId);

        Assert.Equal("Done", (await client.GetFromJsonAsync<JsonElement>($"/api/reports/{reportId}"))
            .GetProperty("status").GetString());

        var bytes = await (await client.GetAsync($"/api/reports/{reportId}/download")).Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        Assert.True(bytes.Length > 1000, $"PDF beklenenden kucuk: {bytes.Length} bayt");

        var outputDir = Environment.GetEnvironmentVariable("LIVE_OUTPUT_DIR");
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
            await File.WriteAllBytesAsync(Path.Combine(outputDir, "rapor.pdf"), bytes);
        }
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
