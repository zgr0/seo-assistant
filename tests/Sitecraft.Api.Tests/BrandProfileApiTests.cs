using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sitecraft.Application.Abstractions;
using Sitecraft.Application.Services;
using Sitecraft.Infrastructure.Persistence;
using SkiaSharp;

namespace Sitecraft.Api.Tests;

/// <summary>
/// Marka profili uclari: alan normalizasyonu, silme, logo ve uretim islerinde profilin
/// cozulmesi (secilen -> sitenin varsayilani -> kiracinin varsayilani).
/// </summary>
public class BrandProfileApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    // --- normalizasyon ---

    [Fact]
    public async Task Fields_are_normalized_on_create_and_patch()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-normalize@example.com");

        var created = await client.PostAsJsonAsync("/api/brand-profiles", new
        {
            name = "  Örnek Marka  ",
            bannedPhrases = new[] { " en  ucuz ", "EN UCUZ", "", "garanti" },
            defaultHashtags = new[] { "seo", "#Örnek Marka", "#SEO", "#", "ai" },
            socialHandles = new Dictionary<string, string?>
            {
                ["instagram"] = "@ornekmarka",
                ["X"] = "https://x.com/ornek_x/",
                ["linkedin"] = "https://www.linkedin.com/company/ornek-as/",
                ["facebook"] = "  "
            },
            primaryColor = "1a2b3c",
            accentColor = "#ffaa00"
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var profile = await created.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Örnek Marka", profile.GetProperty("name").GetString());
        Assert.Equal(["en ucuz", "garanti"], Strings(profile, "bannedPhrases"));
        // Kullanicinin yazimi korunur; tekrar (#SEO) buyuk/kucuk harf farksiz elenir.
        Assert.Equal(["#seo", "#ÖrnekMarka", "#ai"], Strings(profile, "defaultHashtags"));
        Assert.Equal("#1A2B3C", profile.GetProperty("primaryColor").GetString());
        Assert.Equal("#FFAA00", profile.GetProperty("accentColor").GetString());
        Assert.False(profile.GetProperty("hasLogo").GetBoolean());

        var handles = profile.GetProperty("socialHandles");
        Assert.Equal("ornekmarka", handles.GetProperty("instagram").GetString());
        Assert.Equal("ornek_x", handles.GetProperty("x").GetString());
        Assert.Equal("ornek-as", handles.GetProperty("linkedin").GetString());
        Assert.False(handles.TryGetProperty("facebook", out _)); // bos deger platformu kaldirir

        // PATCH: bos dize alani temizler, sozluk tumuyle degisir, verilmeyen alan korunur.
        var id = profile.GetProperty("id").GetGuid();
        var patched = await client.PatchAsJsonAsync($"/api/brand-profiles/{id}", new
        {
            accentColor = "",
            socialHandles = new Dictionary<string, string?> { ["x"] = "@yeni" }
        });

        Assert.Equal(HttpStatusCode.OK, patched.StatusCode);
        var updated = await patched.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, updated.GetProperty("accentColor").ValueKind);
        Assert.Equal("#1A2B3C", updated.GetProperty("primaryColor").GetString());
        Assert.Equal(["x"], updated.GetProperty("socialHandles").EnumerateObject().Select(p => p.Name));
        Assert.Equal(["#seo", "#ÖrnekMarka", "#ai"], Strings(updated, "defaultHashtags"));
    }

    [Theory]
    [InlineData("""{"name":"X","primaryColor":"kirmizi"}""")]
    [InlineData("""{"name":"X","accentColor":"#12345"}""")]
    [InlineData("""{"name":"X","socialHandles":{"tiktok":"ornek"}}""")]
    [InlineData("""{"name":"X","socialHandles":{"instagram":"ornek marka!"}}""")]
    [InlineData("""{"name":"X","defaultHashtags":["a1","a2","a3","a4","a5","a6","a7","a8","a9","a10","a11"]}""")]
    [InlineData("""{"name":"   "}""")]
    public async Task Invalid_fields_are_400(string body)
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, $"brand-invalid-{Guid.NewGuid():N}@example.com");

        var res = await client.PostAsync("/api/brand-profiles",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Long_texts_are_400_instead_of_a_database_error()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-long@example.com");

        var longContext = await client.PostAsJsonAsync("/api/brand-profiles",
            new { name = "X", extraContext = new string('a', 2001) });
        var longName = await client.PostAsJsonAsync("/api/brand-profiles",
            new { name = new string('a', 201) });
        var longPhrase = await client.PostAsJsonAsync("/api/brand-profiles",
            new { name = "X", bannedPhrases = new[] { new string('a', 101) } });

        Assert.Equal(HttpStatusCode.BadRequest, longContext.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longName.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longPhrase.StatusCode);
    }

    // --- silme ---

    [Fact]
    public async Task Delete_removes_the_profile_and_unlinks_its_jobs()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-delete@example.com");
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "brand-delete2@example.com");

        var id = await CreateAsync(client, new { name = "Silinecek" });
        var job = await GenerateAsync(client, id);
        Assert.Equal(id, job.GetProperty("brandProfileId").GetGuid());

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/api/brand-profiles/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/brand-profiles/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/brand-profiles/{id}")).StatusCode);

        // Is kalir, profil baglantisi kopar.
        var reloaded = await client.GetFromJsonAsync<JsonElement>(
            $"/api/content/jobs/{job.GetProperty("id").GetGuid()}");
        Assert.Equal(JsonValueKind.Null, reloaded.GetProperty("brandProfileId").ValueKind);
    }

    // --- logo ---

    [Fact]
    public async Task Logo_is_normalized_to_a_transparent_png_and_can_be_replaced_and_removed()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-logo@example.com");
        var id = await CreateAsync(client, new { name = "Logolu" });

        var uploaded = await client.PutAsync($"/api/brand-profiles/{id}/logo", LogoForm(Png(1024, 400)));
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        Assert.True((await uploaded.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("hasLogo").GetBoolean());

        var logo = await client.GetAsync($"/api/brand-profiles/{id}/logo");
        Assert.Equal(HttpStatusCode.OK, logo.StatusCode);
        Assert.Equal("image/png", logo.Content.Headers.ContentType?.MediaType);

        // Uzun kenar 512'ye iner, oran korunur, seffaf koseler seffaf kalir.
        using (var bitmap = SKBitmap.Decode(await logo.Content.ReadAsByteArrayAsync()))
        {
            Assert.Equal(BrandProfileService.LogoMaxSide, bitmap.Width);
            Assert.Equal(200, bitmap.Height);
            Assert.Equal(0, bitmap.GetPixel(2, 2).Alpha);
            Assert.True(bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2).Alpha > 200);
        }

        // Yeni logo eski dosyanin yerini alir; eski dosya depodan silinir.
        var firstKey = await LogoKeyAsync(factory, id);
        var replaced = await client.PutAsync($"/api/brand-profiles/{id}/logo", LogoForm(Png(300, 300)));
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);

        var secondKey = await LogoKeyAsync(factory, id);
        Assert.NotEqual(firstKey, secondKey);
        Assert.Null(await ReadAssetAsync(factory, firstKey!));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/brand-profiles/{id}/logo")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/brand-profiles/{id}/logo")).StatusCode);
        Assert.Null(await ReadAssetAsync(factory, secondKey!));

        var profile = await client.GetFromJsonAsync<JsonElement>($"/api/brand-profiles/{id}");
        Assert.False(profile.GetProperty("hasLogo").GetBoolean());
    }

    [Fact]
    public async Task Unreadable_unsupported_or_oversized_logo_is_rejected()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-logo-bad@example.com");
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "brand-logo-bad2@example.com");
        var id = await CreateAsync(client, new { name = "Bozuk logo" });

        var garbage = await client.PutAsync($"/api/brand-profiles/{id}/logo", LogoForm([1, 2, 3, 4, 5]));
        Assert.Equal(HttpStatusCode.BadRequest, garbage.StatusCode);

        var svg = Encoding.UTF8.GetBytes("""<svg xmlns="http://www.w3.org/2000/svg" width="10" height="10"/>""");
        var svgRes = await client.PutAsync($"/api/brand-profiles/{id}/logo", LogoForm(svg, "image/svg+xml"));
        Assert.Equal(HttpStatusCode.BadRequest, svgRes.StatusCode);

        var huge = new byte[BrandProfileService.MaxLogoBytes + 1];
        var hugeRes = await client.PutAsync($"/api/brand-profiles/{id}/logo", LogoForm(huge));
        Assert.True(hugeRes.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge,
            $"Beklenmeyen durum: {hugeRes.StatusCode}");

        // Baska kiracinin profiline yazilamaz ve logosu okunamaz.
        var upload = await stranger.PutAsync($"/api/brand-profiles/{id}/logo", LogoForm(Png(64, 64)));
        Assert.Equal(HttpStatusCode.NotFound, upload.StatusCode);

        var own = await client.PutAsync($"/api/brand-profiles/{id}/logo", LogoForm(Png(64, 64)));
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/brand-profiles/{id}/logo")).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_profile_removes_its_logo_file()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-logo-delete@example.com");
        var id = await CreateAsync(client, new { name = "Logo silinir" });

        await client.PutAsync($"/api/brand-profiles/{id}/logo", LogoForm(Png(128, 128)));
        var key = await LogoKeyAsync(factory, id);
        Assert.NotNull(await ReadAssetAsync(factory, key!));

        await client.DeleteAsync($"/api/brand-profiles/{id}");

        Assert.Null(await ReadAssetAsync(factory, key!));
    }

    // --- profil cozumleme ---

    [Fact]
    public async Task Generate_uses_the_tenant_default_unless_disabled()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-resolve@example.com");

        // Profil yok: is profilsiz.
        Assert.Equal(JsonValueKind.Null, (await GenerateAsync(client, null)).GetProperty("brandProfileId").ValueKind);

        var fallback = await CreateAsync(client, new { name = "Varsayilan", isDefault = true });
        var other = await CreateAsync(client, new { name = "Diger" });

        Assert.Equal(fallback, (await GenerateAsync(client, null)).GetProperty("brandProfileId").GetGuid());
        Assert.Equal(other, (await GenerateAsync(client, other)).GetProperty("brandProfileId").GetGuid());
        Assert.Equal(JsonValueKind.Null,
            (await GenerateAsync(client, Guid.Empty)).GetProperty("brandProfileId").ValueKind);
    }

    [Fact]
    public async Task Kit_prefers_the_site_default_and_rejects_another_sites_profile()
    {
        await using var factory = fixture.CreateFactory();
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(factory, webSite, "brand-kit@example.com");

        var tenantDefault = await CreateAsync(client, new { name = "Genel", isDefault = true });
        Assert.Equal(tenantDefault, await KitBrandAsync(client, siteId, null));

        var siteDefault = await CreateAsync(client, new { name = "Site", siteId, isDefault = true });
        Assert.Equal(siteDefault, await KitBrandAsync(client, siteId, null));
        Assert.Null(await KitBrandAsync(client, siteId, Guid.Empty));

        // Kiraci geneli profil her sitede secilebilir; baska siteye bagli profil secilemez.
        Assert.Equal(tenantDefault, await KitBrandAsync(client, siteId, tenantDefault));

        var otherSite = await SiteManagementApiTests.CreateSiteAsync(client, "Diger", "https://diger.example");
        var foreign = await CreateAsync(client, new { name = "Diger site", siteId = otherSite.Id });

        var rejected = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "instagram" },
            brandProfileId = foreign
        });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
    }

    // --- uretim denetimi ---

    /// <summary>Her cagrida ayni yaniti doner — modelin marka kuralina uymadigi durum.</summary>
    private sealed class FakeLlm(string response) : IAnthropicClient
    {
        public bool IsEnabled => true;

        public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default) =>
            Task.FromResult(response);

        public Task<CompletionResult> CompleteDetailedAsync(
            string systemPrompt, string userPrompt, CancellationToken ct = default) =>
            Task.FromResult(new CompletionResult(response, "fake-model", 10, 20));
    }

    [Fact]
    public async Task Kit_falls_back_to_the_template_when_the_model_uses_a_banned_phrase()
    {
        const string banned = """{"variants":[{"angle":"satis_odakli","body":"🚀 Piyasanın EN UCUZ makinası!","hashtags":["#enucuz"],"cta":"Hemen arayın"}]}""";
        await using var factory = fixture.CreateFactory(s => s.AddSingleton<IAnthropicClient>(new FakeLlm(banned)));
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(factory, webSite, "brand-guard-kit@example.com");

        await CreateAsync(client, new { name = "Temkinli", isDefault = true, bannedPhrases = new[] { "en ucuz" } });

        var created = await client.PostAsJsonAsync("/api/social/kits", new { siteId, platformCodes = new[] { "instagram" } });
        var jobId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("jobIds")[0].GetGuid();
        await RunAsync(factory, jobId);

        var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}");
        Assert.Equal("Done", job.GetProperty("status").GetString());

        var variant = job.GetProperty("variants")[0];
        var body = variant.GetProperty("body").GetString()!;
        Assert.DoesNotContain("ucuz", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("🚀", body); // sablon, emojisiz profilde emoji eklemez
    }

    [Fact]
    public async Task Non_social_job_fails_with_a_clear_message_when_every_variant_is_banned()
    {
        const string banned = """{"variants":[{"body":"En ucuz makinalar"},{"body":"EN UCUZ fiyat"}]}""";
        await using var factory = fixture.CreateFactory(s => s.AddSingleton<IAnthropicClient>(new FakeLlm(banned)));
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-guard-title@example.com");

        await CreateAsync(client, new { name = "Temkinli", isDefault = true, bannedPhrases = new[] { "en ucuz" } });

        var jobId = (await GenerateAsync(client, null)).GetProperty("id").GetGuid();
        await RunAsync(factory, jobId);

        var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}");
        Assert.Equal("Failed", job.GetProperty("status").GetString());
        Assert.Contains("yasaklı", job.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public async Task Model_emoji_is_removed_for_a_brand_without_emoji()
    {
        const string withEmoji = """{"variants":[{"body":"✨ Yeni makinalar 🚀","cta":"👉 İnceleyin"}]}""";
        await using var factory = fixture.CreateFactory(s => s.AddSingleton<IAnthropicClient>(new FakeLlm(withEmoji)));
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-guard-emoji@example.com");

        await CreateAsync(client, new { name = "Sade", isDefault = true, emojiUsage = "none" });

        var jobId = (await GenerateAsync(client, null)).GetProperty("id").GetGuid();
        await RunAsync(factory, jobId);

        var variant = (await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}"))
            .GetProperty("variants")[0];
        Assert.Equal("Yeni makinalar", variant.GetProperty("body").GetString());
        Assert.Equal("İnceleyin", variant.GetProperty("cta").GetString());
    }

    // --- gorsel kimlik ---

    [Fact]
    public async Task Kit_images_use_the_brand_color_and_logo()
    {
        await using var factory = fixture.CreateFactory();
        await using var webSite = await TestWebSite.StartAsync();
        var (client, siteId) = await SocialKitApiTests.CrawlForTestsAsync(factory, webSite, "brand-visual@example.com");

        var id = await CreateAsync(client, new { name = "Kırmızı Marka", isDefault = true, primaryColor = "#C8102E" });
        var logo = await client.PutAsync($"/api/brand-profiles/{id}/logo", LogoForm(GreenPng()));
        Assert.Equal(HttpStatusCode.OK, logo.StatusCode);

        var created = await client.PostAsJsonAsync("/api/social/kits", new { siteId, platformCodes = new[] { "instagram" } });
        var jobId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("jobIds")[0].GetGuid();
        await RunAsync(factory, jobId);

        var variant = (await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}")).GetProperty("variants")[0];

        // Test sitesinin fotograflari indirilemez — zincir marka kartina duser; kart markanin renginde.
        using var raw = SKBitmap.Decode(await client.GetByteArrayAsync(
            $"/api/content/assets/{variant.GetProperty("rawImageAssetId").GetGuid()}"));
        var corner = raw.GetPixel(10, 10);
        Assert.True(corner.Red > corner.Green + 40 && corner.Red > corner.Blue + 40, $"Kart kırmızı olmalı: {corner}");

        // Yazili tasarimda marka satirinin basinda logo rozeti.
        using var designed = SKBitmap.Decode(await client.GetByteArrayAsync(
            $"/api/content/assets/{variant.GetProperty("imageAssetId").GetGuid()}"));
        var green = 0;
        for (var y = 0; y < designed.Height; y += 2)
        {
            for (var x = 0; x < designed.Width; x += 2)
            {
                var p = designed.GetPixel(x, y);
                if (p.Green > 150 && p.Red < 90 && p.Blue < 130) green++;
            }
        }
        Assert.True(green > 40, "Logo tasarıma basılmalı");
    }

    // --- yardimcilar ---

    /// <summary>Opak, parlak yesil logo — tasarimda yalniz logodan gelebilecek bir renk.</summary>
    private static byte[] GreenPng()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(240, 96, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(new SKColor(0, 200, 60));

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static async Task RunAsync(WebApplicationFactory<Program> factory, Guid jobId)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ContentService>().RunAsync(jobId);
    }

    private static async Task<Guid> CreateAsync(HttpClient client, object body)
    {
        var res = await client.PostAsJsonAsync("/api/brand-profiles", body);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    /// <summary>Sayfasiz uretim isi — site yok, yalniz kiraci varsayilani aranir.</summary>
    private static async Task<JsonElement> GenerateAsync(HttpClient client, Guid? brandProfileId)
    {
        var res = await client.PostAsJsonAsync("/api/content/generate", new
        {
            type = "title",
            brandProfileId,
            input = new { topic = "Marka testi" }
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<Guid?> KitBrandAsync(HttpClient client, Guid siteId, Guid? brandProfileId)
    {
        var res = await client.PostAsJsonAsync("/api/social/kits", new
        {
            siteId,
            platformCodes = new[] { "instagram" },
            brandProfileId
        });
        Assert.Equal(HttpStatusCode.Accepted, res.StatusCode);

        var jobId = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("jobIds")[0].GetGuid();
        var job = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}");
        var brand = job.GetProperty("brandProfileId");
        return brand.ValueKind == JsonValueKind.Null ? null : brand.GetGuid();
    }

    private static MultipartFormDataContent LogoForm(byte[] bytes, string contentType = "image/png")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", "logo.png" } };
    }

    /// <summary>Seffaf zeminde ortada dolu bir dikdortgen.</summary>
    private static byte[] Png(int width, int height)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        using (var paint = new SKPaint { Color = new SKColor(200, 30, 60) })
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawRect(width * 0.25f, height * 0.25f, width * 0.5f, height * 0.5f, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static async Task<string?> LogoKeyAsync(WebApplicationFactory<Program> factory, Guid id)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SitecraftDbContext>();
        return await db.BrandProfiles.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => p.LogoStorageKey)
            .SingleAsync();
    }

    private static async Task<byte[]?> ReadAssetAsync(WebApplicationFactory<Program> factory, string key)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAssetStorage>().ReadAsync(key);
    }

    private static List<string?> Strings(JsonElement element, string property) =>
        [.. element.GetProperty(property).EnumerateArray().Select(e => e.GetString())];
}
