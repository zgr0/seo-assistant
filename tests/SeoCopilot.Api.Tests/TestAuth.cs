using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SeoCopilot.Api.Tests;

/// <summary>Testler icin kayit olup Bearer basligi hazir bir HttpClient uretir.</summary>
internal static class TestAuth
{
    public static async Task<(HttpClient Client, Guid TenantId)> RegisterAsync(
        WebApplicationFactory<Program> factory, string email)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "sifre12345",
            fullName = "Test Kullanici",
            tenantName = "Test Ajans"
        });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (client, auth.User.TenantId);
    }

    private sealed record AuthResponse(string AccessToken, AuthUser User);

    private sealed record AuthUser(Guid TenantId);
}

/// <summary>/api/sites yanitinin testlerde kullanilan alt kumesi.</summary>
internal sealed record SiteResponse(
    Guid Id,
    string Name,
    string BaseUrl,
    string? VerificationToken,
    DateTimeOffset? VerifiedAt,
    SiteCrawlSettings CrawlSettings);

internal sealed record SiteCrawlSettings(int MaxPages, int MaxDepth, int DelayMs, bool RenderJs, int Concurrency);

internal sealed record VerifyResponse(bool Verified, DateTimeOffset? VerifiedAt, string MetaTag);
