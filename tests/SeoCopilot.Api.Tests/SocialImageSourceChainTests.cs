using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services;
using SeoCopilot.Application.Services.Social;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Json;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Media;
using SeoCopilot.Infrastructure.Persistence;
using SkiaSharp;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Varsayilan (ucretsiz) kaynak zinciri uctan uca: tarama → sayfa fotografi kirpilir ve yazi
/// basilir; fotografi olmayan sayfada marka karti. Yapay zeka uretimi hic cagrilmaz.
/// </summary>
public class SocialImageSourceChainTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    /// <summary>Cagrilirsa test basarisiz — varsayilan zincir ucretli kaynaga gitmemeli.</summary>
    private sealed class ForbiddenGenerator : IImageGenerator
    {
        public int Calls { get; private set; }

        public bool IsEnabled => true;

        public Task<GeneratedImage?> GenerateAsync(string prompt, string aspectRatio, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult<GeneratedImage?>(null);
        }
    }

    [Fact]
    public async Task Article_photo_is_used_and_pages_without_one_get_a_brand_card()
    {
        var generator = new ForbiddenGenerator();

        await using var factory = fixture.CreateFactory(services =>
        {
            services.AddSingleton<IImageGenerator>(generator);
            // Test sitesi 127.0.0.1'de — ic ag korumasi yalniz bu test icin gevsetilir.
            services.Configure<SiteImageFetcherOptions>(o => o.AllowPrivateNetworks = true);
        });

        using var photo = new SKBitmap(1400, 1000);
        using (var canvas = new SKCanvas(photo)) canvas.Clear(new SKColor(70, 110, 140));
        using var encoded = SKImage.FromBitmap(photo).Encode(SKEncodedImageFormat.Jpeg, 90);

        await using var site = await ImageTestServer.StartAsync(encoded.ToArray());
        var (client, siteId) = await CrawlAsync(factory, site.BaseUrl, "free-images@example.com");

        // Tarama gorsel adreslerini saklamali — kaynak zinciri buna dayaniyor.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
            // Ayni siniftaki diger testler de gorselli siteyi tarar — sorgu bu siteyle sinirli.
            var article = await db.Pages.SingleAsync(p =>
                p.Crawl!.SiteId == siteId && p.Url.EndsWith("/blog/fabrika-acilisi"));
            Assert.Contains($"{site.BaseUrl}/uploads/fabrika.jpg", article.ImageUrls);
        }

        var created = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "linkedin" },
            postCount = 2
        });
        var kit = await created.Content.ReadFromJsonAsync<JsonElement>();
        var jobIds = kit.GetProperty("jobIds").EnumerateArray().Select(j => j.GetGuid()).ToList();

        foreach (var jobId in jobIds)
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
        }

        Assert.Equal(0, generator.Calls);

        using var check = factory.Services.CreateScope();
        var context = check.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
        var assets = await context.ContentAssets
            .Include(a => a.Job!).ThenInclude(j => j.Page)
            .Where(a => jobIds.Contains(a.JobId))
            .ToListAsync();

        // Yazi yolu once gelir → fotografli sayfa. Ana sayfada yalniz logo var → kart.
        var articleRaw = Assert.Single(assets, a =>
            a.Kind == ContentAssetKind.Raw && a.Job!.Page!.Url.EndsWith("/blog/fabrika-acilisi"));
        Assert.Equal(SocialImageService.SiteImageModel, articleRaw.Model);
        Assert.Equal($"{site.BaseUrl}/uploads/fabrika.jpg", articleRaw.Prompt);
        // LinkedIn 16:9 — kaynak 1400x1000 kirpilip olceklendi.
        Assert.Equal((1200, 675), (articleRaw.Width, articleRaw.Height));

        var homeRaw = Assert.Single(assets, a =>
            a.Kind == ContentAssetKind.Raw && !a.Job!.Page!.Url.Contains("/blog/"));
        Assert.Equal(SocialImageService.CardModel, homeRaw.Model);

        // Her ikisinin de yazili kopyasi var.
        Assert.Equal(2, assets.Count(a => a.Kind == ContentAssetKind.Captioned));
    }

    /// <summary>Basilacak yaziyi ve sablonu kaydeder; yazili kopya uretmez.</summary>
    private sealed class RecordingComposer : ISocialImageComposer
    {
        public List<ImageCaption> Captions { get; } = [];

        public ComposedImage? Compose(byte[] image, ImageCaption caption)
        {
            lock (Captions) Captions.Add(caption);
            return null;
        }
    }

    [Theory]
    [InlineData("split", ImageTemplate.Split)]
    [InlineData("Poster", ImageTemplate.Poster)]
    public async Task Templates_chosen_in_the_form_are_used_for_every_post(string requested, ImageTemplate expected)
    {
        var composer = new RecordingComposer();

        await using var factory = fixture.CreateFactory(services =>
        {
            services.AddSingleton<IImageGenerator>(new ForbiddenGenerator());
            services.AddSingleton<ISocialImageComposer>(composer);
            services.Configure<SiteImageFetcherOptions>(o => o.AllowPrivateNetworks = true);
        });

        using var photo = new SKBitmap(1400, 1000);
        using (var canvas = new SKCanvas(photo)) canvas.Clear(new SKColor(70, 110, 140));
        using var encoded = SKImage.FromBitmap(photo).Encode(SKEncodedImageFormat.Jpeg, 90);

        await using var site = await ImageTestServer.StartAsync(encoded.ToArray());
        var (client, siteId) = await CrawlAsync(factory, site.BaseUrl, $"template-{requested}@example.com");

        var created = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "linkedin" },
            postCount = 2,
            imageTemplates = new[] { requested }
        });
        var jobIds = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("jobIds").EnumerateArray().Select(j => j.GetGuid()).ToList();

        foreach (var jobId in jobIds)
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
        }

        // Fotografli sayfa da fotografsiz sayfa da secilen sablonu alir.
        Assert.Equal(2, composer.Captions.Count);
        Assert.All(composer.Captions, c => Assert.Equal(expected, c.Template));

        using var check = factory.Services.CreateScope();
        var models = await check.ServiceProvider.GetRequiredService<SeoCopilotDbContext>().ContentAssets
            .Where(a => jobIds.Contains(a.JobId))
            .Select(a => a.Model)
            .ToListAsync();

        // Afis fotograf ustunde okunmaz: yalniz fotografsiz sablon secilince fotograf hic kullanilmaz.
        if (expected == ImageTemplate.Poster)
            Assert.All(models, m => Assert.Equal(SocialImageService.CardModel, m));
        else
            Assert.Contains(SocialImageService.SiteImageModel, models);
    }

    [Fact]
    public async Task Unknown_template_name_is_400()
    {
        await using var factory = fixture.CreateFactory();
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(factory, webSite, "template-unknown@example.com");

        var res = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "instagram" },
            imageTemplates = new[] { "Carousel" }
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, res.StatusCode);
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
}
