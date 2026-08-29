using Microsoft.EntityFrameworkCore;
using SeoCopilot.Domain.Entities.Content;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Performance;
using SeoCopilot.Domain.Entities.Reporting;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Entities.System;
using SeoCopilot.Domain.Entities.Tenancy;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Persistence.Conventions;

namespace SeoCopilot.Infrastructure.Persistence;

public sealed class SeoCopilotDbContext(DbContextOptions<SeoCopilotDbContext> options) : DbContext(options)
{
    // Kiraci & kullanici
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Site & tarama
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Crawl> Crawls => Set<Crawl>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageLink> PageLinks => Set<PageLink>();

    // Kural motoru
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueIgnore> IssueIgnores => Set<IssueIgnore>();

    // Performans
    public DbSet<Vital> Vitals => Set<Vital>();

    // Marka & icerik
    public DbSet<BrandProfile> BrandProfiles => Set<BrandProfile>();
    public DbSet<PlatformProfile> PlatformProfiles => Set<PlatformProfile>();
    public DbSet<ContentJob> ContentJobs => Set<ContentJob>();
    public DbSet<ContentVariant> ContentVariants => Set<ContentVariant>();

    // Rapor & sistem
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    private static readonly Type[] EnumTypes =
    [
        typeof(TenantPlan), typeof(UserRole), typeof(VerificationMethod), typeof(CrawlStatus),
        typeof(CrawlTrigger), typeof(RuleCategory), typeof(Severity), typeof(IssueStatus),
        typeof(VitalsDevice), typeof(VitalsSource), typeof(BrandTone), typeof(AddressForm),
        typeof(EmojiUsage), typeof(ContentJobType), typeof(ContentJobStatus), typeof(ReportStatus)
    ];

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        foreach (var enumType in EnumTypes)
        {
            configurationBuilder.Properties(enumType)
                .HaveConversion(typeof(SnakeCaseEnumConverter<>).MakeGenericType(enumType))
                .HaveMaxLength(32);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SeoCopilotDbContext).Assembly);
    }
}
