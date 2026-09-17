using Microsoft.Extensions.Logging;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Application.Services.Reporting;
using SeoCopilot.Domain.Entities.Reporting;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services;

/// <summary>
/// Rapor uretimi. Istek kaydi acar, uretimi kuyruga atar; dosya hazir olunca
/// <c>storage_key</c> yazilir ve indirme ucu dosyayi doner.
/// </summary>
public sealed class ReportService(
    IReportRepository reports,
    ISiteRepository sites,
    IReportStorage storage,
    IReportQueue queue,
    IPdfRenderer pdf,
    ILogger<ReportService> logger)
{
    private static (string Extension, string ContentType) Output(ReportFormat format) => format switch
    {
        ReportFormat.Pdf => ("pdf", "application/pdf"),
        _ => ("html", "text/html; charset=utf-8")
    };

    public async Task<ReportDto> CreateAsync(
        Guid siteId, Guid tenantId, CreateReportRequest request, CancellationToken ct = default)
    {
        var site = await sites.GetSiteForTenantAsync(siteId, tenantId, ct)
            ?? throw new NotFoundException($"Site {siteId} bulunamadı");

        var crawl = request.CrawlId is Guid crawlId
            ? await sites.GetCrawlForTenantAsync(crawlId, tenantId, ct)
                ?? throw new NotFoundException($"Crawl {crawlId} bulunamadı")
            : await sites.GetLatestCrawlAsync(siteId, ct)
                ?? throw new InvalidOperationException("Site icin raporlanacak tarama yok");

        if (crawl.SiteId != site.Id)
            throw new InvalidOperationException("Tarama bu siteye ait değil");

        if (request.CompareCrawlId is Guid compareId)
        {
            var compare = await sites.GetCrawlForTenantAsync(compareId, tenantId, ct)
                ?? throw new NotFoundException($"Crawl {compareId} bulunamadı");
            if (compare.SiteId != site.Id)
                throw new InvalidOperationException("Kıyaslanan tarama bu siteye ait değil");
        }

        var end = request.PeriodEnd ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = request.PeriodStart
            ?? DateOnly.FromDateTime((crawl.StartedAt ?? crawl.CreatedAt).UtcDateTime);
        if (start > end) (start, end) = (end, start);

        var report = new Report
        {
            SiteId = site.Id,
            CrawlId = crawl.Id,
            CompareCrawlId = request.CompareCrawlId,
            PeriodStart = start,
            PeriodEnd = end,
            Format = EnumText.ParseOptional<ReportFormat>(request.Format, "format") ?? ReportFormat.Pdf,
            Status = ReportStatus.Queued
        };

        await reports.AddReportAsync(report, ct);
        await reports.SaveChangesAsync(ct);

        queue.Enqueue(report.Id);
        return ReportDto.From(report);
    }

    /// <summary>Hangfire worker giris noktasi.</summary>
    public async Task RunAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await reports.GetReportAsync(reportId, ct)
            ?? throw new NotFoundException($"Rapor {reportId} bulunamadı");

        if (report.Status is ReportStatus.Done or ReportStatus.Running) return;

        report.Status = ReportStatus.Running;
        await reports.SaveChangesAsync(ct);

        try
        {
            var site = await sites.GetSiteAsync(report.SiteId, ct)
                ?? throw new NotFoundException($"Site {report.SiteId} bulunamadi");
            var crawl = await sites.GetCrawlAsync(report.CrawlId, ct)
                ?? throw new NotFoundException($"Crawl {report.CrawlId} bulunamadı");

            var issues = await sites.ListIssuesAsync(report.CrawlId, ct);
            var previous = report.CompareCrawlId is Guid compareId
                ? await sites.GetCrawlAsync(compareId, ct)
                : null;

            var html = HtmlReportRenderer.Render(
                site, crawl, issues, previous, report.PeriodStart, report.PeriodEnd);

            // Iki bicim de ayni HTML'den cikar; PDF onun tarayicida basilmis halidir.
            var content = report.Format is ReportFormat.Pdf
                ? await pdf.RenderAsync(html, ct)
                : html;

            var (extension, _) = Output(report.Format);
            report.StorageKey = await storage.SaveAsync($"reports/{report.Id}.{extension}", content, ct);
            report.Status = ReportStatus.Done;
            report.GeneratedAt = DateTimeOffset.UtcNow;
            await reports.SaveChangesAsync(ct);

            logger.LogInformation(
                "Rapor {ReportId} uretildi: {Format}, {Bytes} bayt",
                report.Id, report.Format, content.Length);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rapor {ReportId} uretilemedi", report.Id);
            report.Status = ReportStatus.Failed;
            await reports.SaveChangesAsync(CancellationToken.None);
        }
    }

    public async Task<ReportDto?> GetAsync(Guid reportId, Guid tenantId, CancellationToken ct = default)
    {
        var report = await reports.GetReportForTenantAsync(reportId, tenantId, ct);
        return report is null ? null : ReportDto.From(report);
    }

    public async Task<IReadOnlyList<ReportDto>> ListAsync(
        Guid siteId, Guid tenantId, CancellationToken ct = default)
    {
        _ = await sites.GetSiteForTenantAsync(siteId, tenantId, ct)
            ?? throw new NotFoundException($"Site {siteId} bulunamadı");

        return [.. (await reports.ListReportsAsync(siteId, ct)).Select(ReportDto.From)];
    }

    /// <summary>Rapor dosyasi, onerilen dosya adi ve icerik turu. Rapor hazir degilse 400 dogurur.</summary>
    public async Task<(byte[] Content, string FileName, string ContentType)> DownloadAsync(
        Guid reportId, Guid tenantId, CancellationToken ct = default)
    {
        var report = await reports.GetReportForTenantAsync(reportId, tenantId, ct)
            ?? throw new NotFoundException($"Rapor {reportId} bulunamadı");

        if (report.Status != ReportStatus.Done || report.StorageKey is null)
            throw new InvalidOperationException($"Rapor henüz hazır değil (durum: {report.Status})");

        var content = await storage.ReadAsync(report.StorageKey, ct)
            ?? throw new NotFoundException("Rapor dosyası depoda bulunamadı");

        var (extension, contentType) = Output(report.Format);
        return (content, $"seocopilot-rapor-{report.PeriodEnd:yyyyMMdd}-{report.Id}.{extension}", contentType);
    }
}
