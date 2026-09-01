using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CrawlEngine>();
        services.AddScoped<CrawlOrchestrator>();
        services.AddScoped<SiteService>();
        services.AddScoped<AuthService>();
        services.AddScoped<IssueService>();
        services.AddScoped<BrandProfileService>();
        services.AddScoped<ContentService>();
        services.AddScoped<ReportService>();
        services.AddScoped<VitalsService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
