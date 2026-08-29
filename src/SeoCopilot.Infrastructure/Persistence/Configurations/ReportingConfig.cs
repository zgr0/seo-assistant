using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Reporting;
using SeoCopilot.Domain.Entities.System;

namespace SeoCopilot.Infrastructure.Persistence.Configurations;

internal sealed class ReportConfig : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.ToTable("reports");
        b.HasKey(x => x.Id);
        b.Property(x => x.StorageKey).HasMaxLength(512);

        b.HasIndex(x => new { x.SiteId, x.CrawlId });

        b.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Crawl).WithMany().HasForeignKey(x => x.CrawlId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(64).IsRequired();
        b.Property(x => x.Entity).HasMaxLength(64).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Payload).HasColumnType("jsonb");
        b.Property(x => x.Ip).HasColumnType("inet");
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        b.HasIndex(x => new { x.TenantId, x.CreatedAt }).IsDescending(false, true);
    }
}
