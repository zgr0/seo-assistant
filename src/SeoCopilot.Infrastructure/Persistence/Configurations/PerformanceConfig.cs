using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Performance;

namespace SeoCopilot.Infrastructure.Persistence.Configurations;

internal sealed class VitalConfig : IEntityTypeConfiguration<Vital>
{
    public void Configure(EntityTypeBuilder<Vital> b)
    {
        b.ToTable("vitals");
        b.HasKey(x => x.Id);
        b.Property(x => x.Url).HasMaxLength(2048).IsRequired();
        b.Property(x => x.Cls).HasPrecision(5, 3);
        b.Property(x => x.CollectedAt).HasDefaultValueSql("now()");

        b.HasIndex(x => new { x.SiteId, x.Url, x.CollectedAt }).IsDescending(false, false, true);

        b.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Crawl).WithMany().HasForeignKey(x => x.CrawlId).OnDelete(DeleteBehavior.SetNull);
    }
}
