using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Infrastructure.Persistence.Conventions;

namespace SeoCopilot.Infrastructure.Persistence.Configurations;

internal sealed class CrawlConfig : IEntityTypeConfiguration<Crawl>
{
    public void Configure(EntityTypeBuilder<Crawl> b)
    {
        b.ToTable("crawls");
        b.HasKey(x => x.Id);
        b.Property(x => x.PagesDiscovered).HasDefaultValue(0);
        b.Property(x => x.PagesCrawled).HasDefaultValue(0);
        b.Property(x => x.OverallScore).HasPrecision(5, 2);
        b.Property(x => x.ErrorMessage);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        b.Property(x => x.CategoryScores)
            .HasColumnType("jsonb")
            .HasConversion(JsonbDictionary.Converter<decimal>(), JsonbDictionary.Comparer<decimal>());
        b.Property(x => x.IssueCounts)
            .HasColumnType("jsonb")
            .HasConversion(JsonbDictionary.Converter<int>(), JsonbDictionary.Comparer<int>());
        b.Property(x => x.ScoringSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(JsonbDictionary.Converter<int>(), JsonbDictionary.Comparer<int>());

        b.HasIndex(x => new { x.SiteId, x.CreatedAt }).IsDescending(false, true);

        b.HasOne(x => x.Site).WithMany(s => s.Crawls)
            .HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PageConfig : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> b)
    {
        b.ToTable("pages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Url).HasMaxLength(2048).IsRequired();
        b.Property(x => x.UrlHash).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(128);
        b.Property(x => x.RedirectTo).HasMaxLength(2048);
        b.Property(x => x.Title).HasMaxLength(1024);
        b.Property(x => x.MetaDescription).HasMaxLength(2048);
        b.Property(x => x.CanonicalUrl).HasMaxLength(2048);
        b.Property(x => x.RobotsMeta).HasMaxLength(128);
        b.Property(x => x.Lang).HasMaxLength(16);
        b.Property(x => x.OgData).HasColumnType("jsonb");
        b.Property(x => x.CrawledAt).HasDefaultValueSql("now()");

        b.HasIndex(x => new { x.CrawlId, x.UrlHash }).IsUnique();
        b.HasIndex(x => new { x.CrawlId, x.StatusCode });
        b.HasIndex(x => new { x.CrawlId, x.ContentHash });

        b.HasOne(x => x.Crawl).WithMany(c => c.Pages)
            .HasForeignKey(x => x.CrawlId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PageLinkConfig : IEntityTypeConfiguration<PageLink>
{
    public void Configure(EntityTypeBuilder<PageLink> b)
    {
        b.ToTable("page_links");
        b.HasKey(x => x.Id);
        b.Property(x => x.ToUrl).HasMaxLength(2048).IsRequired();
        b.Property(x => x.AnchorText).HasMaxLength(512);

        b.HasIndex(x => new { x.CrawlId, x.ToPageId });
        b.HasIndex(x => new { x.CrawlId, x.FromPageId });

        b.HasOne(x => x.FromPage).WithMany(p => p.OutLinks)
            .HasForeignKey(x => x.FromPageId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ToPage).WithMany()
            .HasForeignKey(x => x.ToPageId).OnDelete(DeleteBehavior.SetNull);
    }
}
