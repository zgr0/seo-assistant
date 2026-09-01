using System.Net;
using System.Net.Http.Json;

namespace SeoCopilot.Api.Tests;

public class SitesApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Create_site_returns_201_with_normalised_base_url()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "sites-create@example.com");

        var res = await client.PostAsJsonAsync("/api/sites", new { name = "Ornek", baseUrl = "https://Example.com/" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var site = await res.Content.ReadFromJsonAsync<SiteResponse>();
        Assert.NotNull(site);
        Assert.Equal("https://example.com", site!.BaseUrl); // normalize edilir, sonda / yok
        Assert.Equal(500, site.CrawlSettings.MaxPages);
    }

    [Fact]
    public async Task Create_site_with_invalid_url_is_400()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "sites-invalid@example.com");

        var res = await client.PostAsJsonAsync("/api/sites", new { name = "Ornek", baseUrl = "ftp://example.com" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task List_returns_only_own_tenant_sites()
    {
        await using var factory = fixture.CreateFactory();
        var (first, _) = await TestAuth.RegisterAsync(factory, "sites-t1@example.com");
        var (second, _) = await TestAuth.RegisterAsync(factory, "sites-t2@example.com");

        await first.PostAsJsonAsync("/api/sites", new { name = "Birinci", baseUrl = "https://birinci.example" });
        await second.PostAsJsonAsync("/api/sites", new { name = "Ikinci", baseUrl = "https://ikinci.example" });

        var sites = await first.GetFromJsonAsync<List<SiteResponse>>("/api/sites");

        Assert.NotNull(sites);
        Assert.Single(sites!);
        Assert.Equal("Birinci", sites![0].Name);
    }

    [Fact]
    public async Task Other_tenants_site_is_404()
    {
        await using var factory = fixture.CreateFactory();
        var (owner, _) = await TestAuth.RegisterAsync(factory, "sites-owner@example.com");
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "sites-stranger@example.com");

        var created = await owner.PostAsJsonAsync("/api/sites", new { name = "Gizli", baseUrl = "https://gizli.example" });
        var site = await created.Content.ReadFromJsonAsync<SiteResponse>();

        var res = await stranger.GetAsync($"/api/sites/{site!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Sites_endpoints_require_a_token()
    {
        await using var factory = fixture.CreateFactory();
        var anonymous = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/sites")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/sites", new { name = "x", baseUrl = "https://x.example" })).StatusCode);
    }

    [Fact]
    public async Task Crawl_settings_patch_applies_only_given_fields()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "sites-settings@example.com");
        var created = await client.PostAsJsonAsync("/api/sites", new { name = "Ayar", baseUrl = "https://ayar.example" });
        var site = await created.Content.ReadFromJsonAsync<SiteResponse>();

        var res = await client.PatchAsJsonAsync(
            $"/api/sites/{site!.Id}/crawl-settings", new { maxPages = 25, renderJs = true });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var updated = await res.Content.ReadFromJsonAsync<SiteResponse>();
        Assert.Equal(25, updated!.CrawlSettings.MaxPages);
        Assert.True(updated.CrawlSettings.RenderJs);
        Assert.Equal(5, updated.CrawlSettings.MaxDepth); // dokunulmayan alan varsayilanda kalir
    }

    [Fact]
    public async Task Crawl_starts_right_after_the_site_is_created()
    {
        await using var webSite = await TestWebSite.StartAsync();
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "sites-crawl-start@example.com");
        var created = await client.PostAsJsonAsync("/api/sites", new { name = "Yeni", baseUrl = webSite.BaseUrl });
        var site = await created.Content.ReadFromJsonAsync<SiteResponse>();

        var res = await client.PostAsJsonAsync("/api/crawls", new { siteId = site!.Id });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Crawl_for_another_tenants_site_is_404()
    {
        await using var factory = fixture.CreateFactory();
        var (owner, _) = await TestAuth.RegisterAsync(factory, "sites-crawl-owner@example.com");
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "sites-crawl-stranger@example.com");
        var created = await owner.PostAsJsonAsync("/api/sites", new { name = "Gizli", baseUrl = "https://gizli-crawl.example" });
        var site = await created.Content.ReadFromJsonAsync<SiteResponse>();

        var res = await stranger.PostAsJsonAsync("/api/crawls", new { siteId = site!.Id });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
