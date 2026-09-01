using SeoCopilot.Domain.Entities.Reporting;

namespace SeoCopilot.Application.Abstractions;

public interface IReportRepository
{
    Task AddReportAsync(Report report, CancellationToken ct = default);

    Task<Report?> GetReportAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>Kiraci sinirini uygular; report → site → tenant zinciri uzerinden.</summary>
    Task<Report?> GetReportForTenantAsync(Guid reportId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<Report>> ListReportsAsync(Guid siteId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Uretilen rapor dosyalarinin deposu (yerel disk, S3, ...).</summary>
public interface IReportStorage
{
    /// <summary>Iceriği yazar ve depolama anahtarini doner.</summary>
    Task<string> SaveAsync(string key, byte[] content, CancellationToken ct = default);

    /// <summary>Yoksa null doner.</summary>
    Task<byte[]?> ReadAsync(string key, CancellationToken ct = default);
}
