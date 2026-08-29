using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SeoCopilot.Api.Tests;

public class AuthApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static object Register(string email) => new
    {
        email,
        password = "sifre12345",
        fullName = "Test Kullanici",
        tenantName = "Test Ajans"
    };

    [Fact]
    public async Task Register_returns_tokens_and_user()
    {
        using var factory = fixture.CreateFactory();
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/register", Register("owner1@example.com"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.Equal("owner1@example.com", body.User.Email);
        Assert.Equal("owner", body.User.Role);
    }

    [Fact]
    public async Task Duplicate_email_is_409()
    {
        using var factory = fixture.CreateFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", Register("dupe@example.com"));

        var res = await client.PostAsJsonAsync("/api/auth/register", Register("dupe@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Login_wrong_password_is_401()
    {
        using var factory = fixture.CreateFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", Register("login@example.com"));

        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = "login@example.com", password = "yanlis" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_then_access_protected_endpoint()
    {
        using var factory = fixture.CreateFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", Register("crawl@example.com"));

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "crawl@example.com", password = "sifre12345" });
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        // Bilinmeyen crawl -> 404 (401 degil), yani token kabul edildi.
        var res = await client.GetAsync($"/api/crawls/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_token_and_old_one_is_rejected()
    {
        using var factory = fixture.CreateFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", Register("refresh@example.com"));
        var login = await (await client.PostAsJsonAsync("/api/auth/login",
            new { email = "refresh@example.com", password = "sifre12345" })).Content.ReadFromJsonAsync<AuthResponse>();

        var first = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login!.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    private sealed record AuthResponse(string AccessToken, string RefreshToken, AuthUser User);
    private sealed record AuthUser(string Email, string Role);
}
