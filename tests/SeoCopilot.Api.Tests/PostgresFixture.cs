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

    /// <param name="configure">Testin servisleri degistirmesi icin (sahte istemciler vb.).</param>
    public WebApplicationFactory<Program> CreateFactory(Action<IServiceCollection>? configure = null) =>
        new CustomFactory(ConnectionString, configure);

    private sealed class CustomFactory(string connectionString, Action<IServiceCollection>? configure)
        : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            if (configure is not null) builder.ConfigureServices(configure);

            builder.ConfigureHostConfiguration(cfg => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString,

                // Worker kapali: testler isleri ya dogrudan tetikler ya da yalniz kuyruga
                // girdigini dogrular. Acik olsaydi arka plandaki tarama testlerle yarisir ve
                // fabrika sokulurken Hangfire kapanis logu sokulmus logger'a yazmaya calisirdi.
                ["Hangfire:EnableServer"] = "false"
            }));
            return base.CreateHost(builder);
        }
    }
}
