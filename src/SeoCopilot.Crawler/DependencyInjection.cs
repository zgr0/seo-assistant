using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler;

public static class DependencyInjection
{
    public static IServiceCollection AddCrawler(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<CrawlerOptions>(config.GetSection(CrawlerOptions.Section));
        services.AddSingleton<PlaywrightBrowserPool>();

        // Yonlendirmeler PageExtractor icinde elle izlenir — redirect_to kaybolmasin.
        services.AddHttpClient<IPageExtractor, PageExtractor>(ConfigureClient)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All
            });

        services.AddHttpClient<IRobotsSource, RobotsProvider>(ConfigureClient);
        services.AddHttpClient<ISitemapSource, SitemapReader>(ConfigureClient);
        services.AddHttpClient<ISiteVerifier, SiteVerifier>(ConfigureClient);

        return services;
    }

    private static void ConfigureClient(IServiceProvider sp, HttpClient client)
    {
        var options = sp.GetRequiredService<IOptions<CrawlerOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
    }
}
