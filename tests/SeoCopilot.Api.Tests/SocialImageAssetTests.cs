using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Services;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Persistence;
using SkiaSharp;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Gorsel uretimi sahte istemciyle kosulur: FLUX cagrisi yok, ucret yok. Asil olculen sey
/// ayni uretimden hem ham hem yazili varligin dogmasi — yazi icin ikinci uretim YAPILMAMALI.
/// </summary>
public class SocialImageAssetTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    /// <summary>Cagri sayan sahte uretici; her cagride duz renkli bir JPEG doner.</summary>
    private sealed class CountingGenerator : IImageGenerator
    {
        public int Calls { get; private set; }

        public bool IsEnabled => true;

        public Task<GeneratedImage?> GenerateAsync(
            string prompt, string aspectRatio, CancellationToken ct = default)
        {
            Calls++;

            using var bitmap = new SKBitmap(512, 512);
            using (var canvas = new SKCanvas(bitmap)) canvas.Clear(new SKColor(35, 55, 85));
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);

            return Task.FromResult<GeneratedImage?>(
                new GeneratedImage(data.ToArray(), "image/jpeg", 512, 512, "sahte-model"));
        }
    }

    [Fact]
    public async Task One_generation_produces_a_raw_and_a_captioned_asset()
    {
        var generator = new CountingGenerator();

        await using var factory = fixture.CreateFactory(services =>
            services.AddSingleton<IImageGenerator>(generator));
        await using var webSite = await TestWebSite.StartAsync();

        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(
            factory, webSite, "asset-pair@example.com");

        var created = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "instagram" },
            postCount = 1
        });

        var jobId = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("jobIds")[0].GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
        }

        // Tek gorsel, tek uretim cagrisi.
        Assert.Equal(1, generator.Calls);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
            var assets = await db.ContentAssets.Where(a => a.JobId == jobId).ToListAsync();

            Assert.Equal(2, assets.Count);

            var raw = Assert.Single(assets, a => a.Kind == ContentAssetKind.Raw);
            var captioned = Assert.Single(assets, a => a.Kind == ContentAssetKind.Captioned);

            Assert.Null(raw.SourceAssetId);
            Assert.Equal(raw.Id, captioned.SourceAssetId);
            // Gercek olculer yaziliyor (orandan tahmin degil).
            Assert.Equal(512, captioned.Width);
            Assert.NotEqual(raw.Bytes, captioned.Bytes);
        }

        var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}");
        var variant = job.GetProperty("variants")[0];

        var shown = variant.GetProperty("imageAssetId").GetGuid();
        var rawId = variant.GetProperty("rawImageAssetId").GetGuid();
        Assert.NotEqual(shown, rawId);

        // Ikisi de indirilebilir olmali.
        foreach (var id in new[] { shown, rawId })
        {
            var res = await client.GetAsync($"/api/content/assets/{id}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
            Assert.True((await res.Content.ReadAsByteArrayAsync()).Length > 1000);
        }
    }

    [Fact]
    public async Task Overlay_off_leaves_only_the_raw_asset()
    {
        var generator = new CountingGenerator();

        await using var factory = fixture.CreateFactory(services =>
        {
            services.AddSingleton<IImageGenerator>(generator);
            services.AddSingleton<ISocialImageComposer, DisabledComposer>();
        });
        await using var webSite = await TestWebSite.StartAsync();

        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(
            factory, webSite, "asset-nooverlay@example.com");

        var created = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "instagram" },
            postCount = 1
        });
        var jobId = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("jobIds")[0].GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
        }

        using var check = factory.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
        var assets = await db.ContentAssets.Where(a => a.JobId == jobId).ToListAsync();

        Assert.Single(assets);
        Assert.Equal(ContentAssetKind.Raw, assets[0].Kind);

        var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}");
        var variant = job.GetProperty("variants")[0];
        Assert.Equal(assets[0].Id, variant.GetProperty("imageAssetId").GetGuid());
        Assert.Equal(JsonValueKind.Null, variant.GetProperty("rawImageAssetId").ValueKind);
    }

    [Fact]
    public async Task Gallery_lists_each_image_once_newest_first_within_the_tenant()
    {
        var generator = new CountingGenerator();

        await using var factory = fixture.CreateFactory(services =>
            services.AddSingleton<IImageGenerator>(generator));
        await using var webSite = await TestWebSite.StartAsync();

        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(
            factory, webSite, "gallery-owner@example.com");

        // Iki ayri uretim — galeride iki gorsel, her biri yalniz yazili surumuyle.
        var jobIds = new List<Guid>();
        for (var i = 0; i < 2; i++)
        {
            var created = await client.PostAsJsonAsync("/api/social/kits", new
            {
                siteId,
                platformCodes = new[] { "instagram" },
                postCount = 1
            });
            var jobId = (await created.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("jobIds")[0].GetGuid();
            jobIds.Add(jobId);

            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
        }

        var gallery = await client.GetFromJsonAsync<JsonElement>($"/api/content/assets?siteId={siteId}");

        Assert.Equal(2, gallery.GetProperty("total").GetInt32());

        var items = gallery.GetProperty("items").EnumerateArray().ToList();
        Assert.All(items, item =>
        {
            // Ham gorsel ayrica listelenmez; yazili surum ona isaret eder.
            Assert.Equal("Captioned", item.GetProperty("kind").GetString());
            Assert.NotEqual(JsonValueKind.Null, item.GetProperty("rawAssetId").ValueKind);
            Assert.False(string.IsNullOrEmpty(item.GetProperty("pageUrl").GetString()));
            Assert.Equal("instagram", item.GetProperty("platformCode").GetString());
        });

        // Yeniden eskiye: son uretimin gorseli basta.
        Assert.Equal(jobIds[1], items[0].GetProperty("jobId").GetGuid());

        // Sayfalama
        var firstPage = await client.GetFromJsonAsync<JsonElement>(
            $"/api/content/assets?siteId={siteId}&size=1&page=1");
        Assert.Equal(1, firstPage.GetProperty("items").GetArrayLength());
        Assert.Equal(2, firstPage.GetProperty("total").GetInt32());

        // Baska kiraci hicbir sey gormez.
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "gallery-stranger@example.com");
        var foreign = await stranger.GetFromJsonAsync<JsonElement>("/api/content/assets");
        Assert.Equal(0, foreign.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Gallery_shows_the_raw_image_when_no_caption_was_printed()
    {
        await using var factory = fixture.CreateFactory(services =>
        {
            services.AddSingleton<IImageGenerator>(new CountingGenerator());
            services.AddSingleton<ISocialImageComposer, DisabledComposer>();
        });
        await using var webSite = await TestWebSite.StartAsync();

        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(
            factory, webSite, "gallery-raw@example.com");

        var created = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "instagram" },
            postCount = 1
        });
        var jobId = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("jobIds")[0].GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
        }

        var gallery = await client.GetFromJsonAsync<JsonElement>($"/api/content/assets?siteId={siteId}");
        var item = Assert.Single(gallery.GetProperty("items").EnumerateArray());

        Assert.Equal("Raw", item.GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("rawAssetId").ValueKind);
    }

    /// <summary>Yazi basmayi kapatan sahte — ham gorselle devam edilmeli.</summary>
    private sealed class DisabledComposer : ISocialImageComposer
    {
        public ComposedImage? Compose(byte[] image, ImageCaption caption) => null;
    }
}
