using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Infrastructure.Clients;
using SeoCopilot.Infrastructure.Email;
using SeoCopilot.Infrastructure.Persistence;

namespace SeoCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SeoCopilotDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("Postgres")));

        services.AddScoped<ISiteRepository, SiteRepository>();

        services.Configure<AnthropicOptions>(config.GetSection(AnthropicOptions.Section));
        services.Configure<PsiOptions>(config.GetSection(PsiOptions.Section));
        services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.Section));

        services.AddHttpClient<IAnthropicClient, AnthropicClient>();
        services.AddHttpClient<IPageSpeedClient, PsiClient>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
