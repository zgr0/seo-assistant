using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Marka/platform profilleri, icerik uretim isleri ve dashboard. Anthropic anahtari
/// tanimsiz oldugundan uretim isi arka planda 'failed' olur — testler is kaydini ve
/// dogrulamalari olcer, model ciktisini degil.
/// </summary>
public class ContentApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Platform_profiles_return_the_seed_list()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "platforms@example.com");

        var body = await client.GetFromJsonAsync<JsonElement>("/api/platform-profiles");

        var codes = body.EnumerateArray().Select(p => p.GetProperty("code").GetString()).ToList();
        Assert.Equal(["facebook", "instagram", "linkedin", "x"], codes);
        Assert.Equal(280, body.EnumerateArray().First(p => p.GetProperty("code").GetString() == "x")
            .GetProperty("maxChars").GetInt32());
    }

    [Fact]
    public async Task Platform_profiles_require_a_token()
    {
        await using var factory = fixture.CreateFactory();
        var anonymous = factory.CreateClient();

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/api/platform-profiles")).StatusCode);
    }

    [Fact]
    public async Task Brand_profile_create_list_and_patch()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-crud@example.com");

        var created = await client.PostAsJsonAsync("/api/brand-profiles", new
        {
            name = "Ana marka",
            tone = "satis_odakli",
            addressForm = "sen",
            emojiUsage = "light",
            bannedPhrases = new[] { "en ucuz" },
            defaultHashtags = new[] { "#seo" },
            targetAudience = "KOBI sahipleri",
            isDefault = true
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var profile = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SatisOdakli", profile.GetProperty("tone").GetString());
        Assert.Equal("Sen", profile.GetProperty("addressForm").GetString());
        Assert.True(profile.GetProperty("isDefault").GetBoolean());

        var id = profile.GetProperty("id").GetGuid();
        var patched = await client.PatchAsJsonAsync($"/api/brand-profiles/{id}", new { tone = "teknik" });
        var updated = await patched.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Teknik", updated.GetProperty("tone").GetString());
        Assert.Equal("Ana marka", updated.GetProperty("name").GetString()); // dokunulmayan alan korunur

        var list = await client.GetFromJsonAsync<JsonElement>("/api/brand-profiles");
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task Second_default_brand_profile_clears_the_first()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-default@example.com");

        var first = await client.PostAsJsonAsync("/api/brand-profiles", new { name = "Birinci", isDefault = true });
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.PostAsJsonAsync("/api/brand-profiles", new { name = "Ikinci", isDefault = true });

        var reloaded = await client.GetFromJsonAsync<JsonElement>($"/api/brand-profiles/{firstId}");

        Assert.False(reloaded.GetProperty("isDefault").GetBoolean());
    }

    [Fact]
    public async Task Invalid_tone_is_400_and_other_tenants_profile_is_404()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "brand-guard@example.com");
        var (stranger, _) = await TestAuth.RegisterAsync(factory, "brand-guard2@example.com");

        var bad = await client.PostAsJsonAsync("/api/brand-profiles", new { name = "X", tone = "asiri" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var created = await client.PostAsJsonAsync("/api/brand-profiles", new { name = "Gizli" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/brand-profiles/{id}")).StatusCode);
    }

    [Fact]
    public async Task Generate_creates_a_job_that_can_be_polled_and_listed()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "content-generate@example.com");

        var res = await client.PostAsJsonAsync("/api/content/generate", new
        {
            type = "meta_description",
            input = new { currentTitle = "Ornek sayfa", keywords = new[] { "seo", "arac" } },
            variantCount = 2
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var job = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MetaDescription", job.GetProperty("type").GetString());
        Assert.Contains("variantCount", job.GetProperty("input").GetString());

        var jobId = job.GetProperty("id").GetGuid();
        var polled = await client.GetFromJsonAsync<JsonElement>($"/api/content/jobs/{jobId}");
        Assert.Equal(jobId, polled.GetProperty("id").GetGuid());

        var list = await client.GetFromJsonAsync<JsonElement>("/api/content/jobs?type=meta_description");
        Assert.Equal(1, list.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Social_generation_requires_a_platform_and_rejects_unknown_ones()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "content-platform@example.com");

        var missing = await client.PostAsJsonAsync("/api/content/generate", new { type = "social_post" });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        var unknown = await client.PostAsJsonAsync(
            "/api/content/generate", new { type = "social_post", platformCode = "myspace" });
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var ok = await client.PostAsJsonAsync(
            "/api/content/generate", new { type = "social_post", platformCode = "X" });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        Assert.Equal("x", (await ok.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("platformCode").GetString());
    }

    [Fact]
    public async Task Unknown_content_type_is_400()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "content-badtype@example.com");

        var res = await client.PostAsJsonAsync("/api/content/generate", new { type = "sarki_sozu" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Batch_requires_page_ids_and_returns_one_job_per_page()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "content-batch@example.com");

        var empty = await client.PostAsJsonAsync("/api/content/generate-batch", new
        {
            type = "social_post",
            platformCode = "instagram",
            pageIds = Array.Empty<Guid>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        var unknownPage = await client.PostAsJsonAsync("/api/content/generate-batch", new
        {
            type = "social_post",
            platformCode = "instagram",
            pageIds = new[] { Guid.NewGuid() }
        });
        Assert.Equal(HttpStatusCode.NotFound, unknownPage.StatusCode);
    }

    [Fact]
    public async Task Export_csv_returns_a_header_and_one_row_per_job()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "content-export@example.com");

        var created = await client.PostAsJsonAsync("/api/content/generate", new { type = "title" });
        var jobId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var res = await client.GetAsync($"/api/content/export.csv?jobIds={jobId}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("text/csv", res.Content.Headers.ContentType!.MediaType);
        var csv = await res.Content.ReadAsStringAsync();
        Assert.Contains("job_id,type,platform_code", csv);
        Assert.Contains(jobId.ToString(), csv);
    }

    [Fact]
    public async Task Export_csv_without_job_ids_is_400()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "content-export-empty@example.com");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/content/export.csv")).StatusCode);
    }

    [Fact]
    public async Task Favorite_on_an_unknown_variant_is_404()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "content-favorite@example.com");

        var res = await client.PostAsJsonAsync(
            $"/api/content/variants/{Guid.NewGuid()}/favorite", new { isFavorite = true });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Dashboard_summarises_the_tenants_sites()
    {
        await using var factory = fixture.CreateFactory();
        var (client, _) = await TestAuth.RegisterAsync(factory, "dashboard@example.com");
        var site = await SiteManagementApiTests.CreateSiteAsync(client, "Pano", "https://pano.example");
        await client.PostAsJsonAsync("/api/content/generate", new { type = "title" });

        var body = await client.GetFromJsonAsync<JsonElement>("/api/dashboard");

        Assert.Equal(1, body.GetProperty("siteCount").GetInt32());
        Assert.Equal(1, body.GetProperty("sites").GetArrayLength());
        Assert.Equal(site.Id, body.GetProperty("sites")[0].GetProperty("siteId").GetGuid());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("sites")[0].GetProperty("lastCrawlId").ValueKind);
        Assert.Equal(1, body.GetProperty("recentContentJobs").GetArrayLength());
    }
}
