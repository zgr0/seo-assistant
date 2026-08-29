using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Tenancy;

namespace SeoCopilot.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfig : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.PagesUsed).HasDefaultValue(0);
        b.Property(x => x.AiTokensUsed).HasDefaultValue(0L);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
    }
}

internal sealed class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasColumnType("citext").IsRequired();
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(200);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        b.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();

        b.HasOne(x => x.Tenant).WithMany(t => t.Users)
            .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired();
        b.Ignore(x => x.IsActive);
        b.HasIndex(x => new { x.UserId, x.ExpiresAt });
        b.HasIndex(x => x.TokenHash).IsUnique();

        b.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
