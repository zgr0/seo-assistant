using SeoCopilot.Domain.Entities.Performance;
using SeoCopilot.Domain.Entities.Reporting;

namespace SeoCopilot.Application.Dtos;

/// <summary>Crawl verilmezse sitenin en son crawl'i kullanilir.</summary>
public record CreateReportRequest(
    Guid? CrawlId = null,
    Guid? CompareCrawlId = null,
    DateOnly? PeriodStart = null,
    DateOnly? PeriodEnd = null);

public record ReportDto(
    Guid Id,
    Guid SiteId,
    Guid CrawlId,
    Guid? CompareCrawlId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Status,
    DateTimeOffset? GeneratedAt,
    string? DownloadUrl)
{
    public static ReportDto From(Report r) => new(
        r.Id,
        r.SiteId,
        r.CrawlId,
        r.CompareCrawlId,
        r.PeriodStart,
        r.PeriodEnd,
        r.Status.ToString(),
        r.GeneratedAt,
        r.StorageKey is null ? null : $"/api/reports/{r.Id}/download");
}

public record VitalDto(
    long Id,
    Guid SiteId,
    Guid? CrawlId,
    string Url,
    string Device,
    string Source,
    int? LcpMs,
    int? InpMs,
    decimal? Cls,
    int? TtfbMs,
    int? FcpMs,
    int? PerfScore,
    DateTimeOffset CollectedAt)
{
    public static VitalDto From(Vital v) => new(
        v.Id, v.SiteId, v.CrawlId, v.Url, v.Device.ToString(), v.Source.ToString(),
        v.LcpMs, v.InpMs, v.Cls, v.TtfbMs, v.FcpMs, v.PerfScore, v.CollectedAt);
}

/// <summary>GET /sites/{id}/vitals — son olcum + gecmis seri.</summary>
public record SiteVitalsDto(Guid SiteId, VitalDto? Latest, IReadOnlyList<VitalDto> History);
