using Microsoft.EntityFrameworkCore;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Domain.Entities.Reporting;

namespace SeoCopilot.Infrastructure.Persistence;

public sealed class ReportRepository(SeoCopilotDbContext db) : IReportRepository
{
    public async Task AddReportAsync(Report report, CancellationToken ct = default) =>
        await db.Reports.AddAsync(report, ct);

    public Task<Report?> GetReportAsync(Guid reportId, CancellationToken ct = default) =>
        db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct);

    public Task<Report?> GetReportForTenantAsync(
        Guid reportId, Guid tenantId, CancellationToken ct = default) =>
        db.Reports.FirstOrDefaultAsync(r => r.Id == reportId && r.Site!.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<Report>> ListReportsAsync(Guid siteId, CancellationToken ct = default) =>
        await db.Reports
            .Where(r => r.SiteId == siteId)
            .OrderByDescending(r => r.PeriodEnd)
            .ThenByDescending(r => r.Id)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
