using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services;
using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Persistence;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// "Uret"e her basista yeni gonderi: sayfalar dolasilir, ayni metin ikinci kez yazilmaz.
/// Anthropic anahtari tanimsiz — gonderiler sablonla uretilir (en cok tekrar riski olan yol).
/// Ayrica gonderi silme ucu.
/// </summary>
public class SocialPostVarietyTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly PlatformProfile Instagram = new()
    {
        Code = "instagram", DisplayName = "Instagram",
        MaxChars = 2200, RecommendedChars = 150, MaxHashtags = 5, SupportsLinks = false
    };

    [Fact]
    public async Task Generating_again_and_again_never_repeats_a_post()
    {
        await using var factory = fixture.CreateFactory();
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(
            factory, webSite, "social-variety@example.com");

        var bodies = new List<string>();
        var pages = new List<string>();

        // Sitedeki uygun sayfa sayisindan fazla tur: sayfalar tekrar secilmek zorunda.
        for (var round = 0; round < 8; round++)
        {
            var (jobIds, pageUrls) = await CreateKitAsync(client, siteId, ["instagram"], postCount: 1);
            await RunAsync(factory, jobIds);

            pages.AddRange(pageUrls);
            foreach (var jobId in jobIds)
                bodies.Add((await VariantAsync(client, jobId)).GetProperty("body").GetString()!);
        }

        Assert.Equal(bodies.Count, bodies.Select(PostHistory.Normalize).Distinct().Count());

        // Ard arda iki tiklama ayni sayfayi secmez; butun sayfalar dolasilmadan basa donulmez.
        var distinctPages = pages.Distinct().Count();
        Assert.True(distinctPages > 1);
        Assert.Equal(distinctPages, pages.Take(distinctPages).Distinct().Count());
    }

    [Fact]
    public async Task Two_platforms_of_the_same_page_get_different_posts()
    {
        await using var factory = fixture.CreateFactory();
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(
            factory, webSite, "social-variety-platforms@example.com");

        // Ikisi de link destekler ve siniri genistir: sablon ikisine ayni metni yazabilirdi.
        var (jobIds, _) = await CreateKitAsync(client, siteId, ["facebook", "linkedin"], postCount: 1);
        await RunAsync(factory, jobIds);

        var first = (await VariantAsync(client, jobIds[0])).GetProperty("body").GetString()!;
        var second = (await VariantAsync(client, jobIds[1])).GetProperty("body").GetString()!;

        Assert.NotEqual(PostHistory.Normalize(first), PostHistory.Normalize(second));
    }

    [Fact]
    public void Template_rounds_produce_distinct_posts_for_the_same_page()
    {
        var page = new Page
        {
            Url = "https://ornek.com/urunler/servo-a6",
            Title = "Servo A6 Enjeksiyon Makinası",
            MetaDescription = "Servo motorlu A6 serisi ile enerji tüketimini düşürün.",
            MainText = "A6 serisi servo motor sayesinde yüzde kırka varan enerji tasarrufu sağlar. " +
                "Kapalı devre hidrolik sistem sessiz ve kararlı çalışır. " +
                "Kalıp kapama ünitesi yüksek hassasiyetle konumlanır. " +
                "Dokunmatik kontrol paneli üretim verilerini anlık gösterir."
        };

        var bodies = Enumerable.Range(0, 27)
            .Select(index => PostHistory.Normalize(PagePostBuilder.Build(page, Instagram, null, index).Body))
            .ToList();

        Assert.Equal(bodies.Count, bodies.Distinct().Count());
    }

    [Fact]
    public async Task Deleting_a_post_removes_the_job_variants_and_image_files()
    {
        await using var factory = fixture.CreateFactory();
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(
            factory, webSite, "social-delete@example.com");
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "social-delete-stranger@example.com");

        var (jobIds, _) = await CreateKitAsync(client, siteId, ["instagram"], postCount: 1);
        await RunAsync(factory, jobIds);
        var jobId = jobIds[0];

        List<string> keys;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
            keys = await db.ContentAssets.Where(a => a.JobId == jobId).Select(a => a.StorageKey).ToListAsync();
        }
        Assert.NotEmpty(keys);

        // Baska kiracinin gonderisi gorunmez.
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/api/content/jobs/{jobId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/content/jobs/{jobId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/content/jobs/{jobId}")).StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
            Assert.False(await db.ContentVariants.AnyAsync(v => v.JobId == jobId));
            Assert.False(await db.ContentAssets.AnyAsync(a => a.JobId == jobId));

            var storage = scope.ServiceProvider.GetRequiredService<IAssetStorage>();
            foreach (var key in keys)
                Assert.Null(await storage.ReadAsync(key));
        }

        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/content/jobs/{jobId}")).StatusCode);
    }

    [Fact]
    public async Task A_post_still_being_generated_cannot_be_deleted()
    {
        await using var factory = fixture.CreateFactory();
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(
            factory, webSite, "social-delete-running@example.com");

        // Worker kapali: is 'queued' kalir.
        var (jobIds, _) = await CreateKitAsync(client, siteId, ["instagram"], postCount: 1);

        var res = await client.DeleteAsync($"/api/content/jobs/{jobIds[0]}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    private static async Task<(List<Guid> JobIds, List<string> PageUrls)> CreateKitAsync(
        HttpClient client, Guid siteId, string[] platformCodes, int postCount)
    {
        var res = await client.PostAsJsonAsync("/api/social/kits", new { siteId, platformCodes, postCount });
        Assert.Equal(HttpStatusCode.Accepted, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (
            [.. body.GetProperty("jobIds").EnumerateArray().Select(j => j.GetGuid())],
            [.. body.GetProperty("pageUrls").EnumerateArray().Select(u => u.GetString()!)]);
    }

    /// <summary>Isler sirayla calisir — gercekte de her is bir oncekinin gecmisini gorur.</summary>
    private static async Task RunAsync(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory, IEnumerable<Guid> jobIds)
    {
        foreach (var jobId in jobIds)
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
        }
    }

    private static async Task<JsonElement> VariantAsync(HttpClient client, Guid jobId)
    {
        var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}");
        Assert.Equal("Done", job.GetProperty("status").GetString());
        return job.GetProperty("variants")[0];
    }
}
