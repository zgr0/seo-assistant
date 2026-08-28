using Hangfire.Dashboard;

namespace SeoCopilot.Api.Infrastructure;

/// <summary>
/// Dev'de dashboard herkese acik; prod'da sadece kimligi dogrulanmis kullanici.
/// Prod icin rol/claim kontrolu eklenebilir.
/// </summary>
public sealed class HangfireDashboardAuthFilter(IWebHostEnvironment env) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        if (env.IsDevelopment()) return true;
        return context.GetHttpContext().User.Identity?.IsAuthenticated == true;
    }
}
