using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services;
using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Json;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Media;
using SeoCopilot.Infrastructure.Persistence;
using SkiaSharp;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Formdaki "Yapay zeka ile uret" dugmesi (<c>aiImages: true</c>): kaynak sirasi AI -> site -> kart,
/// AI dusunce site fotografina gecis, dogrulamalar ve gunluk sinir. Uretici sahtedir, ucret yok.
/// </summary>
public class SocialKitAiImageTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string FakeModel = "sahte-ai";

    public enum Outcome { Image, Null, Throw }

    /// <summary>Istenen sonucu veren, cagrilari ve istemleri kaydeden sahte uretici.</summary>
    private sealed class FakeGenerator(Outcome outcome) : IImageGenerator
    {
        private int _calls;

        public int Calls => _calls;

        public bool IsEnabled => true;

        public Task<GeneratedImage?> GenerateAsync(string prompt, string aspectRatio, CancellationToken ct = default)
        {
            Interlocked.Increment(ref _calls);

            return outcome switch
            {
                Outcome.Image => Task.FromResult<GeneratedImage?>(
                    new GeneratedImage(Jpeg(1024, 576, new SKColor(20, 40, 80)), "image/jpeg", 1024, 576, FakeModel)),
                Outcome.Null => Task.FromResult<GeneratedImage?>(null),
                _ => throw new HttpRequestException("Cloudflare erişilemedi")
            };
        }
    }

    [Fact]
    public async Task Ai_button_generates_every_post_image_with_the_model()
    {
        var generator = new FakeGenerator(Outcome.Image);
        await using var factory = CreateFactory(generator);
        await using var site = await ImageTestServer.StartAsync(Jpeg(1400, 1000, new SKColor(70, 110, 140)));
        var (client, siteId) = await CrawlAsync(factory, site.BaseUrl, "ai-kit@example.com");

        var jobIds = await CreateKitAsync(client, siteId, postCount: 2, aiImages: true);
        await RunAsync(factory, jobIds);

        // Makale sayfasinda site fotografi olsa da AI once gelir.
        Assert.Equal(2, generator.Calls);

        var raws = await RawAssetsAsync(factory, jobIds);
        Assert.Equal(2, raws.Count);
        Assert.All(raws, a => Assert.Equal(FakeModel, a.Model));
        // Istem kaydedilir: site fotografinda adres, AI'da yazisiz gorsel tarifi.
        Assert.All(raws, a => Assert.Contains("no text", a.Prompt));

        var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobIds[0]}");
        using var input = JsonDocument.Parse(job.GetProperty("input").GetString()!);
        Assert.Equal("ai", input.RootElement.GetProperty(SocialImageSettings.InputKey).GetString());

        var gallery = await client.GetFromJsonAsync<JsonElement>($"/api/content/assets?siteId={siteId}");
        Assert.All(gallery.GetProperty("items").EnumerateArray(),
            item => Assert.Equal("ai", item.GetProperty("source").GetString()));
    }

    [Theory]
    [InlineData(Outcome.Null)]
    [InlineData(Outcome.Throw)]
    public async Task Failed_ai_falls_back_to_the_site_photo(Outcome outcome)
    {
        var generator = new FakeGenerator(outcome);
        await using var factory = CreateFactory(generator);
        await using var site = await ImageTestServer.StartAsync(Jpeg(1400, 1000, new SKColor(70, 110, 140)));
        var (client, siteId) = await CrawlAsync(factory, site.BaseUrl, $"ai-fallback-{outcome}@example.com");

        var jobIds = await CreateKitAsync(client, siteId, postCount: 2, aiImages: true);
        await RunAsync(factory, jobIds);

        Assert.Equal(2, generator.Calls);

        foreach (var jobId in jobIds)
        {
            var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}");
            Assert.Equal("Done", job.GetProperty("status").GetString());
            Assert.NotEqual(JsonValueKind.Null, job.GetProperty("variants")[0].GetProperty("imageAssetId").ValueKind);
        }

        var raws = await RawAssetsAsync(factory, jobIds);

        // Fotografli makale site fotografini alir; ana sayfada yalniz logo var → marka karti.
        var article = Assert.Single(raws, a => a.Job!.Page!.Url.EndsWith("/blog/fabrika-acilisi"));
        Assert.Equal(SocialImageService.SiteImageModel, article.Model);
        Assert.Equal($"{site.BaseUrl}/uploads/fabrika.jpg", article.Prompt);

        var home = Assert.Single(raws, a => !a.Job!.Page!.Url.Contains("/blog/"));
        Assert.Equal(SocialImageService.CardModel, home.Model);
    }

    [Fact]
    public async Task Normal_button_never_calls_the_model()
    {
        var generator = new FakeGenerator(Outcome.Image);
        await using var factory = CreateFactory(generator);
        await using var site = await ImageTestServer.StartAsync(Jpeg(1400, 1000, new SKColor(70, 110, 140)));
        var (client, siteId) = await CrawlAsync(factory, site.BaseUrl, "ai-off-kit@example.com");

        var jobIds = await CreateKitAsync(client, siteId, postCount: 2, aiImages: false);
        await RunAsync(factory, jobIds);

        Assert.Equal(0, generator.Calls);

        var settings = await client.GetFromJsonAsync<JsonElement>("/api/social/image-settings");
        Assert.Equal(0, settings.GetProperty("usedToday").GetInt32());
    }

    [Fact]
    public async Task Ai_button_is_400_when_cloudflare_is_not_configured()
    {
        // Testlerde Cloudflare anahtari yok — gercek istemci kapali.
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "ai-disabled@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Kapalı", "https://kapali.example");

        var res = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId = site.Id,
            platformCodes = new[] { "instagram" },
            aiImages = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var settings = await client.GetFromJsonAsync<JsonElement>("/api/social/image-settings");
        Assert.False(settings.GetProperty("aiEnabled").GetBoolean());
    }

    [Fact]
    public async Task Ai_button_with_only_card_templates_is_400()
    {
        await using var factory = CreateFactory(new FakeGenerator(Outcome.Image));
        var (client, _) = await TestAuth.RegisterAsync(factory, "ai-card-only@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Afiş", "https://afis.example");

        var res = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId = site.Id,
            platformCodes = new[] { "instagram" },
            imageTemplates = new[] { "Poster", "Quote" },
            aiImages = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Daily_limit_rejects_the_kit_and_opens_no_jobs()
    {
        await using var factory = CreateFactory(new FakeGenerator(Outcome.Image), dailyLimit: 3);
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(factory, webSite, "ai-limit@example.com");

        Assert.Equal(2, (await CreateKitAsync(client, siteId, postCount: 2, aiImages: true)).Count);

        // 1 hak kaldi, paket 2 istiyor.
        var over = await PostKitAsync(client, siteId, postCount: 2, aiImages: true);
        Assert.Equal(HttpStatusCode.Conflict, over.StatusCode);

        // Yapay zekasiz paket sinira takilmaz ve sayilmaz.
        Assert.Single(await CreateKitAsync(client, siteId, postCount: 1, aiImages: false));

        Assert.Single(await CreateKitAsync(client, siteId, postCount: 1, aiImages: true));

        var full = await PostKitAsync(client, siteId, postCount: 1, aiImages: true);
        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);

        var jobs = await client.GetFromJsonAsync<JsonElement>("/api/content/jobs?type=social_kit");
        Assert.Equal(4, jobs.GetProperty("total").GetInt32());

        var settings = await client.GetFromJsonAsync<JsonElement>("/api/social/image-settings");
        Assert.True(settings.GetProperty("aiEnabled").GetBoolean());
        Assert.Equal(3, settings.GetProperty("dailyLimit").GetInt32());
        Assert.Equal(3, settings.GetProperty("usedToday").GetInt32());

        // Sinir kiraci basinadir.
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "ai-limit-stranger@example.com");
        var foreign = await stranger.GetFromJsonAsync<JsonElement>("/api/social/image-settings");
        Assert.Equal(0, foreign.GetProperty("usedToday").GetInt32());
    }

    private WebApplicationFactory<Program> CreateFactory(IImageGenerator generator, int dailyLimit = 20) =>
        fixture.CreateFactory(services =>
        {
            services.AddSingleton(generator);
            // Varsayilan ucretsiz sira korunur — AI yalniz dugmeyle acilmali.
            services.AddSingleton(new SocialImageSettings { MaxAiImagesPerDay = dailyLimit });
            // Test sitesi 127.0.0.1'de — ic ag korumasi yalniz bu test icin gevsetilir.
            services.Configure<SiteImageFetcherOptions>(o => o.AllowPrivateNetworks = true);
        });

    private static Task<HttpResponseMessage> PostKitAsync(
        HttpClient client, Guid siteId, int postCount, bool aiImages) =>
        client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "linkedin" },
            postCount,
            aiImages
        });

    private static async Task<List<Guid>> CreateKitAsync(
        HttpClient client, Guid siteId, int postCount, bool aiImages)
    {
        var res = await PostKitAsync(client, siteId, postCount, aiImages);
        Assert.Equal(HttpStatusCode.Accepted, res.StatusCode);

        return [.. (await res.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("jobIds").EnumerateArray().Select(j => j.GetGuid())];
    }

    private static async Task RunAsync(WebApplicationFactory<Program> factory, IEnumerable<Guid> jobIds)
    {
        foreach (var jobId in jobIds)
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
        }
    }

    private static async Task<List<ContentAsset>> RawAssetsAsync(
        WebApplicationFactory<Program> factory, List<Guid> jobIds)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>().ContentAssets
            .Include(a => a.Job!).ThenInclude(j => j.Page)
            .Where(a => jobIds.Contains(a.JobId) && a.Kind == ContentAssetKind.Raw)
            .ToListAsync();
    }

    private static async Task<(HttpClient Client, Guid SiteId)> CrawlAsync(
        WebApplicationFactory<Program> factory, string baseUrl, string email)
    {
        var (client, _) = await TestAuth.RegisterAsync(factory, email);
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Görselli site", baseUrl);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();

        var entity = await db.Sites.FirstAsync(s => s.Id == site.Id);
        entity.CrawlSettings = new CrawlSettings { DelayMs = 0 };

        var crawl = new Crawl { SiteId = site.Id, Status = CrawlStatus.Queued, Trigger = CrawlTrigger.Manual };
        db.Crawls.Add(crawl);
        await db.SaveChangesAsync();

        await scope.ServiceProvider.GetRequiredService<CrawlOrchestrator>().RunAsync(crawl.Id);
        return (client, site.Id);
    }

    private static byte[] Jpeg(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }
}
