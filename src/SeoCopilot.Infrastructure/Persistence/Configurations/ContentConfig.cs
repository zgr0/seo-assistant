using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Content;

namespace SeoCopilot.Infrastructure.Persistence.Configurations;

internal sealed class BrandProfileConfig : IEntityTypeConfiguration<BrandProfile>
{
    public void Configure(EntityTypeBuilder<BrandProfile> b)
    {
        b.ToTable("brand_profiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.TargetAudience).HasMaxLength(1024);
        b.Property(x => x.IsDefault).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        b.HasIndex(x => x.TenantId);

        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class PlatformProfileConfig : IEntityTypeConfiguration<PlatformProfile>
{
    public void Configure(EntityTypeBuilder<PlatformProfile> b)
    {
        b.ToTable("platform_profiles");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(32);
        b.Property(x => x.DisplayName).HasMaxLength(64).IsRequired();
        b.Property(x => x.GuidanceTr).IsRequired();
        b.Property(x => x.IsActive).HasDefaultValue(true);
    }
}

internal sealed class ContentJobConfig : IEntityTypeConfiguration<ContentJob>
{
    public void Configure(EntityTypeBuilder<ContentJob> b)
    {
        b.ToTable("content_jobs");
        b.HasKey(x => x.Id);
        b.Property(x => x.PlatformCode).HasMaxLength(32);
        b.Property(x => x.Input).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.ErrorMessage);
        b.Property(x => x.Model).HasMaxLength(64);
        b.Property(x => x.CostUsd).HasPrecision(10, 6);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        b.HasIndex(x => new { x.TenantId, x.CreatedAt }).IsDescending(false, true);

        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Page).WithMany().HasForeignKey(x => x.PageId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.BrandProfile).WithMany().HasForeignKey(x => x.BrandProfileId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Platform).WithMany(p => p.ContentJobs).HasForeignKey(x => x.PlatformCode).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ContentVariantConfig : IEntityTypeConfiguration<ContentVariant>
{
    public void Configure(EntityTypeBuilder<ContentVariant> b)
    {
        b.ToTable("content_variants");
        b.HasKey(x => x.Id);
        b.Property(x => x.Angle).HasMaxLength(64);
        b.Property(x => x.Body).IsRequired();
        b.Property(x => x.Cta).HasMaxLength(256);

        b.HasIndex(x => new { x.JobId, x.VariantIndex });

        b.HasOne(x => x.Job).WithMany(j => j.Variants)
            .HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}
