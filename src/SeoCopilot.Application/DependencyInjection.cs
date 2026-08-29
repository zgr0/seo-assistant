using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CrawlOrchestrator>();
        services.AddScoped<AuthService>();
        return services;
    }
}
