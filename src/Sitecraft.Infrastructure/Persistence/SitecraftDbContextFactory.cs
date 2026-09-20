using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sitecraft.Infrastructure.Persistence;

/// <summary>
/// Yalniz tasarim zamani (dotnet ef migrations). Calisma zamaninda
/// Infrastructure.DependencyInjection.AddInfrastructure kullanilir.
/// </summary>
public sealed class SitecraftDbContextFactory : IDesignTimeDbContextFactory<SitecraftDbContext>
{
    public SitecraftDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SITECRAFT_DB")
            ?? "Host=localhost;Port=5432;Database=sitecraft;Username=sitecraft;Password=sitecraft";

        var options = new DbContextOptionsBuilder<SitecraftDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(SitecraftDbContextFactory).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new SitecraftDbContext(options);
    }
}
