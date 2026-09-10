using System.Net;
using System.Net.Http.Json;
using System.Text;
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
using SeoCopilot.Infrastructure.Persistence;
using Xunit.Abstractions;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// GERCEK API cagrisi yapar (Anthropic + FLUX) ve ucret dogurur. Anahtarlar ortam
/// degiskeninde tanimli degilse test sessizce atlanir, bu yuzden normal kosuda calismaz:
///
///   $env:Anthropic__ApiKey = "..."; $env:Flux__ApiKey = "..."
///   dotnet test --filter "FullyQualifiedName~LiveGenerationTests"
///
/// LIVE_OUTPUT_DIR tanimliysa uretilen metin ve gorsel oraya yazilir (goz denetimi icin).
/// </summary>
public class LiveGenerationTests(PostgresFixture fixture, ITestOutputHelper output)
    : IClassFixture<PostgresFixture>
{
    /// <summary>
    /// Uctan uca paket: tarama → uc farkli sayfadan uc gonderi → her birine bir gorsel.
    /// Anthropic anahtari yoksa metin sablonla uretilir, is yine 'done' biter.
    /// </summary>
    [Fact]
    [Trait("Category", "Live")]
    public async Task Social_kit_produces_three_posts_from_three_pages()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Flux__ApiKey")))
        {
            output.WriteLine("Flux__ApiKey tanımsız — canlı test atlandı.");
            return;
        }

        await using var factory = fixture.CreateFactory();

        var target = Environment.GetEnvironmentVariable("LIVE_CRAWL_URL");
        await using var webSite = string.IsNullOrWhiteSpace(target) ? await TestWebSite.StartAsync() : null;
        var baseUrl = target ?? webSite!.BaseUrl;
        output.WriteLine($"taranan site: {baseUrl}");

        var (client, _) = await TestAuth.RegisterAsync(factory, "live-kit@example.com");
        var siteId = await CrawlSiteAsync(factory, baseUrl, "live-kit@example.com", client);

        var created = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "instagram" },
            postCount = 3
        });
        Assert.Equal(HttpStatusCode.Accepted, created.StatusCode);

        var kit = await created.Content.ReadFromJsonAsync<JsonElement>();
        var jobIds = kit.GetProperty("jobIds").EnumerateArray().Select(j => j.GetGuid()).ToList();
        Assert.Equal(3, jobIds.Count);

        // Hangfire testlerde kapali — worker'i dogrudan cagir.
        foreach (var jobId in jobIds)
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
        }

        var outputDir = Environment.GetEnvironmentVariable("LIVE_OUTPUT_DIR");
        if (!string.IsNullOrWhiteSpace(outputDir)) Directory.CreateDirectory(outputDir);

        var pages = new List<string>();
        var summary = new StringBuilder();

        for (var i = 0; i < jobIds.Count; i++)
        {
            var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobIds[i]}");

            Assert.Equal("Done", job.GetProperty("status").GetString());

            var pageUrl = job.GetProperty("pageUrl").GetString()!;
            pages.Add(pageUrl);

            var variant = job.GetProperty("variants")[0];
            var body = variant.GetProperty("body").GetString()!;
            var hashtags = variant.GetProperty("hashtags").EnumerateArray()
                .Select(h => h.GetString()).ToList();

            Assert.False(string.IsNullOrWhiteSpace(body));

            output.WriteLine($"--- gönderi {i + 1} ({job.GetProperty("model")}) ---");
            output.WriteLine($"sayfa: {pageUrl}");
            output.WriteLine($"açı: {variant.GetProperty("angle")}");
            output.WriteLine(body);
            output.WriteLine($"hashtag: {string.Join(' ', hashtags)}");

            summary.AppendLine($"### Gönderi {i + 1} — {variant.GetProperty("angle")}");
            summary.AppendLine($"sayfa: {pageUrl}");
            summary.AppendLine($"üretim: {job.GetProperty("model")}");
            summary.AppendLine();
            summary.AppendLine(body);
            summary.AppendLine();
            summary.AppendLine($"hashtag: {string.Join(' ', hashtags)}");
            summary.AppendLine($"açıklama: {variant.GetProperty("description")}");
            summary.AppendLine($"görsel alt: {variant.GetProperty("imageAlt")}");
            summary.AppendLine();

            var assetId = variant.GetProperty("imageAssetId");
            Assert.NotEqual(JsonValueKind.Null, assetId.ValueKind);

            var image = await client.GetAsync($"/api/content/assets/{assetId.GetGuid()}");
            Assert.Equal(HttpStatusCode.OK, image.StatusCode);

            var bytes = await image.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 10_000, $"görsel çok küçük: {bytes.Length} bayt");
            output.WriteLine($"görsel: {bytes.Length} bayt");

            if (!string.IsNullOrWhiteSpace(outputDir))
                await File.WriteAllBytesAsync(Path.Combine(outputDir, $"kit-gorsel-{i + 1}.jpg"), bytes);
        }

        // Uc gonderi uc ayri sayfadan gelmeli — cesitlilik sayfalardan doguyor.
        Assert.Equal(3, pages.Distinct().Count());

        if (!string.IsNullOrWhiteSpace(outputDir))
            await File.WriteAllTextAsync(Path.Combine(outputDir, "kit-gonderiler.md"), summary.ToString());
    }

    /// <summary>
    /// LLM'siz yol: gercek tarama verisinden brief uretilir ve dogrudan FLUX'a gonderilir.
    /// Anthropic anahtari gerekmez.
    /// </summary>
    [Fact]
    [Trait("Category", "Live")]
    public async Task Crawl_data_becomes_an_image_without_the_llm()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Flux__ApiKey")))
        {
            output.WriteLine("Flux__ApiKey tanımsız — canlı test atlandı.");
            return;
        }

        await using var factory = fixture.CreateFactory();

        // LIVE_CRAWL_URL verilmezse yerel test sitesi taranir.
        var target = Environment.GetEnvironmentVariable("LIVE_CRAWL_URL");
        await using var webSite = string.IsNullOrWhiteSpace(target)
            ? await TestWebSite.StartAsync()
            : null;

        var baseUrl = target ?? webSite!.BaseUrl;
        output.WriteLine($"taranan site: {baseUrl}");

        var siteId = await CrawlSiteAsync(factory, baseUrl, "live-nollm@example.com");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();

        var page = await db.Pages
            .Where(p => p.Crawl!.SiteId == siteId && p.StatusCode == 200)
            .OrderBy(p => p.Depth)
            .FirstAsync();

        var prompt = PageBriefBuilder.Build(page);
        output.WriteLine($"sayfa: {page.Url}");
        output.WriteLine($"başlık: {page.Title}");
        output.WriteLine($"istem: {prompt}");

        Assert.Contains("no text", prompt);
        Assert.DoesNotContain("modern workspace representing", prompt); // sayfadan konu çıkmalı

        var image = await scope.ServiceProvider.GetRequiredService<IImageGenerator>()
            .GenerateAsync(prompt, "1:1");

        Assert.NotNull(image);
        Assert.True(image.Content.Length > 10_000, $"görsel çok küçük: {image.Content.Length} bayt");
        output.WriteLine($"görsel: {image.Content.Length} bayt, {image.Width}x{image.Height}");

        var outputDir = Environment.GetEnvironmentVariable("LIVE_OUTPUT_DIR");
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
            await File.WriteAllBytesAsync(Path.Combine(outputDir, "taramadan-gorsel.jpg"), image.Content);
            await File.WriteAllTextAsync(
                Path.Combine(outputDir, "taramadan-istem.txt"),
                $"sayfa: {page.Url}\nbaşlık: {page.Title}\n\nistem:\n{prompt}\n");
        }
    }

    /// <summary>Yalniz FLUX yolunu dener — Anthropic anahtari olmadan da kosar.</summary>
    [Fact]
    [Trait("Category", "Live")]
    public async Task Flux_generates_an_image_from_a_brief()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Flux__ApiKey")))
        {
            output.WriteLine("Flux__ApiKey tanımsız — canlı test atlandı.");
            return;
        }

        await using var factory = fixture.CreateFactory();
        using var scope = factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IImageGenerator>();

        Assert.True(generator.IsEnabled);

        var image = await generator.GenerateAsync(
            "A bright minimalist desk with a laptop showing a website analytics dashboard, " +
            "soft morning light, shallow depth of field, no text, no watermark, no logo",
            "1:1");

        Assert.NotNull(image);
        Assert.StartsWith("image/", image.ContentType);
        Assert.True(image.Content.Length > 10_000, $"görsel çok küçük: {image.Content.Length} bayt");
        output.WriteLine($"görsel: {image.Content.Length} bayt, {image.Width}x{image.Height}, {image.Model}");

        var outputDir = Environment.GetEnvironmentVariable("LIVE_OUTPUT_DIR");
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
            await File.WriteAllBytesAsync(Path.Combine(outputDir, "flux-dogrudan.jpg"), image.Content);
        }
    }

    private static async Task<(HttpClient Client, Guid SiteId)> CrawlAsync(
        WebApplicationFactory<Program> factory, TestWebSite webSite, string email)
    {
        var (client, _) = await TestAuth.RegisterAsync(factory, email);
        var siteId = await CrawlSiteAsync(factory, webSite.BaseUrl, email, client);
        return (client, siteId);
    }

    /// <summary>
    /// Siteyi tarar. Yerel test sitesinde bekleme yoktur; disaridaki gercek bir siteye
    /// karsi kosuluyorsa sayfa sayisi ve derinlik kisilir, istekler arasi bekleme konur.
    /// </summary>
    private static async Task<Guid> CrawlSiteAsync(
        WebApplicationFactory<Program> factory, string baseUrl, string email, HttpClient? existing = null)
    {
        var client = existing ?? (await TestAuth.RegisterAsync(factory, email)).Client;
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Canlı test", baseUrl);

        var external = !baseUrl.Contains("127.0.0.1", StringComparison.Ordinal)
            && !baseUrl.Contains("localhost", StringComparison.Ordinal);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>();
            var entity = await db.Sites.FirstAsync(s => s.Id == site.Id);
            entity.CrawlSettings = external
                ? new CrawlSettings { MaxPages = 10, MaxDepth = 1, DelayMs = 500, Concurrency = 1 }
                : new CrawlSettings { DelayMs = 0 };
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

        return site.Id;
    }
}
