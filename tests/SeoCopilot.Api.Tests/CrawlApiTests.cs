using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Postgres'i Testcontainers ile kaldirir, gercek WebApplicationFactory ile HTTP uzerinden dener.
/// <see cref="PostgresFixture"/> collection'a bagli.
/// </summary>
public class CrawlApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Health_endpoint_is_anonymous_and_ok()
    {
        await using var factory = fixture.CreateFactory();
        var client = factory.CreateClient();

        var res = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Post_crawl_without_token_is_401()
    {
        await using var factory = fixture.CreateFactory();
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/crawls", new { siteId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
