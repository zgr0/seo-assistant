using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeoCopilot.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SeoCopilot.Api.Tests;

/// <summary>Test sinifi omru boyunca tek bir Postgres konteyneri; sema migration ile kurulur.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("seocopilot")
        .WithUsername("seocopilot")
        .WithPassword("seocopilot")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>().Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public WebApplicationFactory<Program> CreateFactory() => new CustomFactory(ConnectionString);

    private sealed class CustomFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(cfg => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString
            }));
            return base.CreateHost(builder);
        }
    }
}
