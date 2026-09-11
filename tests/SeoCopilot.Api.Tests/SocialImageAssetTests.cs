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

    /// <summary>Yazi basmayi kapatan sahte — ham gorselle devam edilmeli.</summary>
    private sealed class DisabledComposer : ISocialImageComposer
    {
        public ComposedImage? Compose(byte[] image, ImageCaption caption) => null;
    }
}
