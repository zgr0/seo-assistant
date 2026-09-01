using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Infrastructure.Persistence;

namespace SeoCopilot.Api.Tests;

/// <summary>PATCH/DELETE /sites ve site altindaki tarama listesi.</summary>
public class SiteManagementApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Patch_updates_only_the_given_fields()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "patch-name@example.com");
        var site = await CreateSiteAsync(client, "Eski", "https://patch-name.example");

        var res = await client.PatchAsJsonAsync($"/api/sites/{site.Id}", new { name = "Yeni" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var updated = await res.Content.ReadFromJsonAsync<SiteResponse>();
        Assert.Equal("Yeni", updated!.Name);
        Assert.Equal(site.BaseUrl, updated.BaseUrl);
    }

    [Fact]
    public async Task Patch_normalises_the_new_base_url()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "patch-url@example.com");
        var site = await CreateSiteAsync(client, "Adres", "https://eski.example");

        var res = await client.PatchAsJsonAsync($"/api/sites/{site.Id}", new { baseUrl = "https://Yeni.example/" });

        var updated = await res.Content.ReadFromJsonAsync<SiteResponse>();
        Assert.Equal("https://yeni.example", updated!.BaseUrl);
    }

    [Fact]
    public async Task Patch_with_invalid_base_url_is_400()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "patch-invalid@example.com");
        var site = await CreateSiteAsync(client, "Gecersiz", "https://gecersiz.example");

        var res = await client.PatchAsJsonAsync($"/api/sites/{site.Id}", new { baseUrl = "ftp://x.example" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_the_site()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "delete-site@example.com");
        var site = await CreateSiteAsync(client, "Silinecek", "https://silinecek.example");

        var res = await client.DeleteAsync($"/api/sites/{site.Id}");

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/sites/{site.Id}")).StatusCode);
    }

    [Fact]
    public async Task Other_tenant_cannot_patch_or_delete()
    {
        await using var factory = fixture.CreateFactory();
        var (owner, _) = await TestAuth.RegisterAsync(factory, "guard-owner@example.com");
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "guard-stranger@example.com");
        var site = await CreateSiteAsync(owner, "Korumali", "https://korumali.example");

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await stranger.PatchAsJsonAsync($"/api/sites/{site.Id}", new { name = "Hop" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await stranger.DeleteAsync($"/api/sites/{site.Id}")).StatusCode);
    }

    [Fact]
    public async Task Site_crawls_endpoint_is_paged_and_scoped()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "site-crawls@example.com");
        var site = await CreateSiteAsync(client, "Listeleme", "https://listeleme.example");
        await SeedCrawlsAsync(factory, site.Id, 3);

        var body = await client.GetFromJsonAsync<JsonElement>($"/api/sites/{site.Id}/crawls?page=1&size=2");

        Assert.Equal(3, body.GetProperty("total").GetInt32());
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
        Assert.Equal(2, body.GetProperty("size").GetInt32());
    }

    [Fact]
    public async Task Vitals_endpoint_returns_an_empty_series_when_psi_never_ran()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "site-vitals@example.com");
        var site = await CreateSiteAsync(client, "Vitals", "https://vitals.example");

        var body = await client.GetFromJsonAsync<JsonElement>($"/api/sites/{site.Id}/vitals");

        Assert.Equal(JsonValueKind.Null, body.GetProperty("latest").ValueKind);
        Assert.Equal(0, body.GetProperty("history").GetArrayLength());
    }

    // --- yardimcilar ---

    internal static async Task<SiteResponse> CreateSiteAsync(HttpClient client, string name, string baseUrl)
    {
        var res = await client.PostAsJsonAsync("/api/sites", new { name, baseUrl });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<SiteResponse>())!;
    }

    /// <summary>Hangfire'i tetiklemeden dogrudan crawl satiri yazar.</summary>
    internal static async Task<List<Guid>> SeedCrawlsAsync(
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory, Guid siteId, int count)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();

        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            var crawl = new SeoCopilot.Domain.Entities.Crawling.Crawl
            {
                SiteId = siteId,
                Status = SeoCopilot.Domain.Enums.CrawlStatus.Queued
            };
            db.Crawls.Add(crawl);
            ids.Add(crawl.Id);
        }

        await db.SaveChangesAsync();
        return ids;
    }
}
