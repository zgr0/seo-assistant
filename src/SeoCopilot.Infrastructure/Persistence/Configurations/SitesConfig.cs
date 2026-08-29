using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Sites;

namespace SeoCopilot.Infrastructure.Persistence.Configurations;

internal sealed class SiteConfig : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> b)
    {
        b.ToTable("sites");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.BaseUrl).HasMaxLength(2048).IsRequired();
        b.Property(x => x.VerificationToken).HasMaxLength(128);
        b.Property(x => x.ScheduleCron).HasMaxLength(64);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        // crawl_settings -> jsonb
        b.OwnsOne(x => x.CrawlSettings, o => o.ToJson());

        b.HasIndex(x => new { x.TenantId, x.IsActive });

        b.HasOne<SeoCopilot.Domain.Entities.Tenancy.Tenant>().WithMany()
            .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
