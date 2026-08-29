using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Rules;

namespace SeoCopilot.Infrastructure.Persistence.Configurations;

internal sealed class RuleConfig : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> b)
    {
        b.ToTable("rules");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(64);
        b.Property(x => x.TitleTr).HasMaxLength(200).IsRequired();
        b.Property(x => x.DescriptionTr).IsRequired();
        b.Property(x => x.HowToFixTr).IsRequired();
        b.Property(x => x.DocUrl).HasMaxLength(512);
        b.Property(x => x.IsActive).HasDefaultValue(true);
    }
}

internal sealed class IssueConfig : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> b)
    {
        b.ToTable("issues");
        b.HasKey(x => x.Id);
        b.Property(x => x.RuleCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.Status).HasDefaultValue(Domain.Enums.IssueStatus.Open);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        b.OwnsOne(x => x.Evidence, o => o.ToJson());

        b.HasIndex(x => new { x.CrawlId, x.Severity });
        b.HasIndex(x => new { x.SiteId, x.RuleCode, x.Status });

        b.HasOne(x => x.Crawl).WithMany(c => c.Issues)
            .HasForeignKey(x => x.CrawlId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Site).WithMany()
            .HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Page).WithMany()
            .HasForeignKey(x => x.PageId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Rule).WithMany(r => r.Issues)
            .HasForeignKey(x => x.RuleCode).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class IssueIgnoreConfig : IEntityTypeConfiguration<IssueIgnore>
{
    public void Configure(EntityTypeBuilder<IssueIgnore> b)
    {
        b.ToTable("issue_ignores");
        b.HasKey(x => x.Id);
        b.Property(x => x.RuleCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.UrlPattern).HasMaxLength(512);
        b.Property(x => x.Reason).HasMaxLength(1024);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        b.HasIndex(x => new { x.SiteId, x.RuleCode });

        b.HasOne(x => x.Site).WithMany()
            .HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.CreatedByUser).WithMany()
            .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.NoAction);
    }
}
