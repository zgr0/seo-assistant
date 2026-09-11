using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Infrastructure.Auth;
using SeoCopilot.Infrastructure.Clients;
using SeoCopilot.Infrastructure.Email;
using SeoCopilot.Infrastructure.Media;
using SeoCopilot.Infrastructure.Persistence;
using SeoCopilot.Infrastructure.Reporting;
using SeoCopilot.Infrastructure.Storage;

namespace SeoCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SeoCopilotDbContext>(o => o
            .UseNpgsql(
                config.GetConnectionString("Postgres"),
                npg => npg.MigrationsAssembly(typeof(SeoCopilotDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<ISiteRepository, SiteRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        services.Configure<ReportStorageOptions>(config.GetSection(ReportStorageOptions.Section));
        services.AddSingleton<IReportStorage, FileReportStorage>();

        services.Configure<AssetStorageOptions>(config.GetSection(AssetStorageOptions.Section));
        services.AddSingleton<IAssetStorage, FileAssetStorage>();

        services.Configure<ImageOverlayOptions>(config.GetSection(ImageOverlayOptions.Section));
        services.AddSingleton<ISocialImageComposer, SkiaImageComposer>();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.Configure<AnthropicOptions>(config.GetSection(AnthropicOptions.Section));
        services.Configure<PsiOptions>(config.GetSection(PsiOptions.Section));
        services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.Section));

        services.Configure<FluxOptions>(config.GetSection(FluxOptions.Section));

        services.AddHttpClient<IAnthropicClient, AnthropicClient>();
        services.AddHttpClient<IPageSpeedClient, PsiClient>();
        services.AddHttpClient<IImageGenerator, FluxImageClient>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
