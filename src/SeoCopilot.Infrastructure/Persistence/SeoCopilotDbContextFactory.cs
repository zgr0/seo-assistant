using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SeoCopilot.Infrastructure.Persistence;

/// <summary>
/// Yalniz tasarim zamani (dotnet ef migrations). Calisma zamaninda
/// Infrastructure.DependencyInjection.AddInfrastructure kullanilir.
/// </summary>
public sealed class SeoCopilotDbContextFactory : IDesignTimeDbContextFactory<SeoCopilotDbContext>
{
    public SeoCopilotDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SEOCOPILOT_DB")
            ?? "Host=localhost;Port=5432;Database=seocopilot;Username=seocopilot;Password=seocopilot";

        var options = new DbContextOptionsBuilder<SeoCopilotDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(SeoCopilotDbContextFactory).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new SeoCopilotDbContext(options);
    }
}
