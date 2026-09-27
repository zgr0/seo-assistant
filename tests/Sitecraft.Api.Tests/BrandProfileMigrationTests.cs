using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Sitecraft.Infrastructure.Persistence;

namespace Sitecraft.Api.Tests;

/// <summary>
/// Yukseltme yolu: <c>sites.default_brand_profile_id</c> kalkarken sitenin sectigi profil
/// varsayilan isaretine tasinmali, kapsam basina tek varsayilan kalmali ve eski satirlar yeni
/// kolonlarla okunabilmeli.
/// </summary>
public class BrandProfileMigrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string BeforeBrandIdentity = "20260917064457_AddReportFormat";

    [Fact]
    public async Task Site_default_moves_to_profile_and_one_default_remains_per_scope()
    {
        var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = $"legacy_{Guid.NewGuid():N}"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<SitecraftDbContext>()
            .UseNpgsql(connection, npg => npg.MigrationsAssembly(typeof(SitecraftDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new SitecraftDbContext(options);
        var migrator = db.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync(BeforeBrandIdentity);

            var tenant = Guid.NewGuid();
            var siteA = Guid.NewGuid();
            var siteB = Guid.NewGuid();

            // A sitesi: eski varsayilan (ayni site) yerine sitede secili profil kazanmali.
            var pickedForA = Guid.NewGuid();
            var oldDefaultOfA = Guid.NewGuid();

            // B sitesi kiraci geneli bir profili gosteriyor — tasinmaz.
            var tenantWidePickedByB = Guid.NewGuid();

            // Kiraci genelinde iki varsayilan — yalniz en yenisi kalir.
            var olderTenantDefault = Guid.NewGuid();
            var newerTenantDefault = Guid.NewGuid();

            await db.Database.ExecuteSqlRawAsync(
                """
                SET session_replication_role = replica;
                INSERT INTO sites (id, tenant_id, name, base_url, crawl_settings, default_brand_profile_id)
                VALUES ({0}, {2}, 'A', 'https://a.com', '{{}}', {3}),
                       ({1}, {2}, 'B', 'https://b.com', '{{}}', {5});
                INSERT INTO brand_profiles (id, tenant_id, site_id, name, tone, address_form, emoji_usage,
                    banned_phrases, default_hashtags, is_default, created_at)
                VALUES ({3}, {2}, {0}, 'A secili', 'kurumsal', 'siz', 'none', '{{}}', '{{}}', false, now() - interval '3 day'),
                       ({4}, {2}, {0}, 'A eski', 'kurumsal', 'siz', 'none', '{{}}', '{{}}', true, now() - interval '2 day'),
                       ({5}, {2}, NULL, 'Genel secili', 'samimi', 'sen', 'light', '{{}}', '{{}}', false, now() - interval '5 day'),
                       ({6}, {2}, NULL, 'Genel eski', 'kurumsal', 'siz', 'none', '{{}}', '{{}}', true, now() - interval '4 day'),
                       ({7}, {2}, NULL, 'Genel yeni', 'kurumsal', 'siz', 'none', '{{}}', '{{}}', true, now() - interval '1 day');
                SET session_replication_role = origin;
                """,
                siteA, siteB, tenant, pickedForA, oldDefaultOfA, tenantWidePickedByB,
                olderTenantDefault, newerTenantDefault);

            await migrator.MigrateAsync();

            var profiles = await db.BrandProfiles.AsNoTracking()
                .Where(p => p.TenantId == tenant)
                .ToDictionaryAsync(p => p.Id);

            Assert.True(profiles[pickedForA].IsDefault);
            Assert.False(profiles[oldDefaultOfA].IsDefault);
            Assert.False(profiles[tenantWidePickedByB].IsDefault);
            Assert.False(profiles[olderTenantDefault].IsDefault);
            Assert.True(profiles[newerTenantDefault].IsDefault);

            // Eski satirlar yeni kolonlarla okunur: bos sozluk, renk/logo yok.
            Assert.All(profiles.Values, p =>
            {
                Assert.Empty(p.SocialHandles);
                Assert.Null(p.PrimaryColor);
                Assert.Null(p.LogoStorageKey);
            });

            // Tekil indeks: ayni kapsama ikinci varsayilan yazilamaz.
            var duplicate = await Assert.ThrowsAsync<PostgresException>(() =>
                db.Database.ExecuteSqlRawAsync(
                    "UPDATE brand_profiles SET is_default = true WHERE id = {0}", olderTenantDefault));
            Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
