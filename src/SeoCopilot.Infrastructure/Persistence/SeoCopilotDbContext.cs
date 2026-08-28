using Microsoft.EntityFrameworkCore;
using SeoCopilot.Domain.Entities;

namespace SeoCopilot.Infrastructure.Persistence;

public sealed class SeoCopilotDbContext(DbContextOptions<SeoCopilotDbContext> options) : DbContext(options)
{
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Crawl> Crawls => Set<Crawl>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Finding> Findings => Set<Finding>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Site>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            e.Property(x => x.OwnerEmail).HasMaxLength(320).IsRequired();
            e.HasMany(x => x.Crawls).WithOne().HasForeignKey(c => c.SiteId);
        });

        b.Entity<Crawl>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasMany(x => x.Pages).WithOne().HasForeignKey(p => p.CrawlId);
        });

        b.Entity<Page>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            e.Property(x => x.Title).HasMaxLength(1024);
            e.Property(x => x.MetaDescription).HasMaxLength(2048);
            e.HasMany(x => x.Findings).WithOne().HasForeignKey(f => f.PageId);
        });

        b.Entity<Finding>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RuleCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Message).HasMaxLength(1024);
        });
    }
}
