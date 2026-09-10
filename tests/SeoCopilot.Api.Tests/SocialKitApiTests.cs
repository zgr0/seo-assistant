using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Services;
using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Json;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Persistence;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Sosyal medya paketi ucu. FLUX ve Anthropic anahtarlari tanimsiz oldugundan uretim
/// arka planda 'failed' olur — testler is kaydini, sayfa secimini ve dogrulamalari olcer.
/// </summary>
public class SocialKitApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Kit_creates_one_job_per_platform_and_selected_page()
    {
        await using var factory = fixture.CreateFactory();
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await CrawlAsync(factory, webSite, "social-kit@example.com");

        var res = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "instagram", "x" },
            postCount = 2
        });

        Assert.Equal(HttpStatusCode.Accepted, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();

        // 2 platform x 2 sayfa
        Assert.Equal(4, body.GetProperty("jobIds").GetArrayLength());
        Assert.Equal(2, body.GetProperty("pageUrls").GetArrayLength());
        Assert.Equal(siteId, body.GetProperty("siteId").GetGuid());

        var jobId = body.GetProperty("jobIds")[0].GetGuid();
        var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}");
        Assert.Equal("SocialKit", job.GetProperty("type").GetString());
        Assert.False(string.IsNullOrEmpty(job.GetProperty("pageUrl").GetString()));

        var list = await client.GetFromJsonAsync<JsonElement>("/api/content/jobs?type=social_kit");
        Assert.Equal(4, list.GetProperty("total").GetInt32());
    }

    /// <summary>
    /// Anthropic anahtari tanimsiz: gonderiler sablonla uretilmeli, is 'done' bitmeli.
    /// FLUX de tanimsiz oldugundan varyant gorselsiz kalir.
    /// </summary>
    [Fact]
    public async Task Kit_falls_back_to_templates_when_the_model_is_off()
    {
        await using var factory = fixture.CreateFactory();
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await CrawlAsync(factory, webSite, "social-kit-template@example.com");

        var created = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "instagram" },
            postCount = 3
        });

        var jobIds = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("jobIds").EnumerateArray().Select(j => j.GetGuid()).ToList();
        Assert.Equal(3, jobIds.Count);

        foreach (var jobId in jobIds)
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
        }

        var angles = new List<string?>();
        var pages = new List<string?>();

        foreach (var jobId in jobIds)
        {
            var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}");

            Assert.Equal("Done", job.GetProperty("status").GetString());
            Assert.Equal("template", job.GetProperty("model").GetString());
            Assert.Equal(0, job.GetProperty("tokensIn").GetInt32());

            var variant = job.GetProperty("variants")[0];
            Assert.False(string.IsNullOrWhiteSpace(variant.GetProperty("body").GetString()));
            Assert.Equal(JsonValueKind.Null, variant.GetProperty("imageAssetId").ValueKind);

            angles.Add(variant.GetProperty("angle").GetString());
            pages.Add(job.GetProperty("pageUrl").GetString());
        }

        // Uc ayri sayfa, uc ayri aci.
        Assert.Equal(3, pages.Distinct().Count());
        Assert.Equal(3, angles.Distinct().Count());
    }

    [Fact]
    public void Selector_skips_legal_and_functional_pages()
    {
        var pages = new List<Page>
        {
            NewPage("https://ornek.com/kisisel-verilerin-korunmasi", depth: 1, inlinks: 90),
            NewPage("https://ornek.com/gizlilik-politikasi", depth: 1, inlinks: 90),
            NewPage("https://ornek.com/sepet", depth: 1, inlinks: 80),
            NewPage("https://ornek.com/urunler", depth: 1, inlinks: 5)
        };

        var selected = PageSelector.Select(pages, 3);

        // Footer sayfalari ic link sayisiyla tepeye cikmamali.
        Assert.Single(selected);
        Assert.Equal("https://ornek.com/urunler", selected[0].Url);
    }

    private static Page NewPage(string url, int depth, int inlinks) => new()
    {
        Url = url,
        StatusCode = 200,
        Depth = depth,
        InlinkCount = inlinks,
        Title = "Başlık",
        MainText = new string('a', 500)
    };

    [Fact]
    public async Task Kit_selects_the_home_page_first()
    {
        await using var factory = fixture.CreateFactory();
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await CrawlAsync(factory, webSite, "social-kit-order@example.com");

        var res = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "linkedin" },
            postCount = 1
        });

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"{webSite.BaseUrl}/", body.GetProperty("pageUrls")[0].GetString());
    }

    [Fact]
    public async Task Kit_without_a_crawl_is_400_and_unknown_site_is_404()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "social-kit-nocrawl@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Taranmamis", "https://yok.example");

        var noCrawl = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId = site.Id,
            platformCodes = new[] { "instagram" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, noCrawl.StatusCode);

        var unknownSite = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId = Guid.NewGuid(),
            platformCodes = new[] { "instagram" }
        });
        Assert.Equal(HttpStatusCode.NotFound, unknownSite.StatusCode);
    }

    [Fact]
    public async Task Kit_validates_platforms_and_caps_the_job_count()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "social-kit-guard@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Sinir", "https://sinir.example");

        var empty = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId = site.Id,
            platformCodes = Array.Empty<string>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        // 4 platform x 5 gonderi = 20 > 12
        var tooMany = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId = site.Id,
            platformCodes = new[] { "instagram", "facebook", "x", "linkedin" },
            postCount = 5
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);

        var unknownPlatform = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId = site.Id,
            platformCodes = new[] { "myspace" }
        });
        Assert.Equal(HttpStatusCode.NotFound, unknownPlatform.StatusCode);
    }

    [Fact]
    public async Task Kit_of_another_tenants_site_is_404()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "social-kit-owner@example.com");
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "social-kit-stranger@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Gizli", "https://gizli.example");

        var res = await stranger.PostAsJsonAsync("/api/social/kits", new
        {
            siteId = site.Id,
            platformCodes = new[] { "instagram" }
        });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Unknown_asset_is_404()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "social-kit-asset@example.com");

        var res = await client.GetAsync($"/api/content/assets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private static async Task<(HttpClient Client, Guid SiteId)> CrawlAsync(
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

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
            var crawl = new Crawl { SiteId = site.Id, Status = CrawlStatus.Queued, Trigger = CrawlTrigger.Manual };
            db.Crawls.Add(crawl);
            await db.SaveChangesAsync();

            await scope.ServiceProvider.GetRequiredService<CrawlOrchestrator>().RunAsync(crawl.Id);
        }

        return (client, site.Id);
    }
}
