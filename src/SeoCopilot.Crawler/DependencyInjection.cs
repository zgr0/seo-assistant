using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler;

public static class DependencyInjection
{
    public static IServiceCollection AddCrawler(this IServiceCollection services)
    {
        services.AddSingleton<PlaywrightBrowserPool>();
        services.AddHttpClient<IPageExtractor, PageExtractor>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(30);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("SeoCopilotBot/1.0 (+https://seocopilot.local/bot)");
        });
        services.AddHttpClient<SitemapReader>();
        return services;
    }
}
