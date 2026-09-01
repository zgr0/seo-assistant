using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Dtos;

namespace SeoCopilot.Application.Services;

/// <summary>Kiracinin acilis ekrani: site kartlari, son taramalar ve son icerik uretimleri.</summary>
public sealed class DashboardService(ISiteRepository sites, IContentRepository content)
{
    public const int RecentCrawlCount = 10;
    public const int RecentContentJobCount = 5;

    public async Task<DashboardDto> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var siteList = await sites.ListSitesAsync(tenantId, ct);

        var cards = new List<DashboardSiteDto>(siteList.Count);
        var issueTotals = new Dictionary<string, int>();
        var scores = new List<decimal>();

        foreach (var site in siteList)
        {
            // Son iki tarama: biri kartin skoru, digeri degisim oku icin.
            var recent = await sites.ListCrawlsAsync(site.Id, 0, 2, ct);
            var last = recent.FirstOrDefault();
            var previous = recent.Skip(1).FirstOrDefault();

            if (last?.OverallScore is decimal score) scores.Add(score);

            if (last is not null)
            {
                foreach (var (severity, count) in last.IssueCounts)
                    issueTotals[severity] = issueTotals.GetValueOrDefault(severity) + count;
            }

            cards.Add(new DashboardSiteDto(
                site.Id,
                site.Name,
                site.BaseUrl,
                site.IsActive,
                last?.Id,
                last?.Status.ToString(),
                last?.OverallScore,
                last?.OverallScore is decimal now && previous?.OverallScore is decimal before
                    ? now - before
                    : null,
                last?.FinishedAt ?? last?.StartedAt ?? last?.CreatedAt,
                last?.IssueCounts ?? new Dictionary<string, int>()));
        }

        var recentCrawls = await sites.ListRecentCrawlsForTenantAsync(tenantId, RecentCrawlCount, ct);
        var (jobs, _) = await content.ListContentJobsAsync(
            tenantId, null, null, null, null, null, 0, RecentContentJobCount, ct);

        return new DashboardDto(
            siteList.Count,
            scores.Count > 0 ? Math.Round(scores.Average(), 1) : null,
            issueTotals,
            cards,
            [.. recentCrawls.Select(CrawlListItemDto.From)],
            [.. jobs.Select(ContentJobDto.From)]);
    }
}
