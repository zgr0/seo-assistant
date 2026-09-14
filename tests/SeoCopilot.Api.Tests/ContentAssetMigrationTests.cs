using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SeoCopilot.Domain.Enums;
using SeoCopilot.Infrastructure.Persistence;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Yukseltme yolu: gorsel yazisi eklenmeden once uretilmis varliklar yeni surumde okunabilmeli.
/// Temiz veritabaninda kosan diger testler bu durumu hic gormez — satir eski semaya yazilip
/// migration zinciri uzerinden gecirilir.
/// </summary>
public class ContentAssetMigrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string BeforeCaptions = "20260910064854_AddSocialKitAssets";
    private const string AddCaptions = "20260911180729_AddCaptionedImageAssets";

    [Fact]
    public async Task Assets_created_before_captions_are_backfilled_as_raw()
    {
        var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = $"legacy_{Guid.NewGuid():N}"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<SeoCopilotDbContext>()
            .UseNpgsql(connection, npg => npg.MigrationsAssembly(typeof(SeoCopilotDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new SeoCopilotDbContext(options);
        var migrator = db.GetService<IMigrator>();

        try
        {
            // 1) Yazi basma oncesi sema — kind kolonu yok.
            await migrator.MigrateAsync(BeforeCaptions);

            var assetId = Guid.NewGuid();
            // Yabanci anahtar tetikleyicileri kapatilir: yalniz varlik satiri onemli.
            await db.Database.ExecuteSqlRawAsync(
                """
                SET session_replication_role = replica;
                INSERT INTO content_assets (id, tenant_id, job_id, storage_key, content_type, width, height, bytes, created_at)
                VALUES ({0}, gen_random_uuid(), gen_random_uuid(), 'eski.jpg', 'image/jpeg', 1024, 1024, 1000, now());
                SET session_replication_role = origin;
                """,
                assetId);

            // 2) Yazi basma migration'i — hatali varsayilan '' yazar.
            await migrator.MigrateAsync(AddCaptions);
            Assert.Equal(string.Empty, await KindOf(db, assetId));

            // 3) Guncel sema — eski satir ham gorsel olarak duzelir ve EF ile okunur.
            await migrator.MigrateAsync();
            Assert.Equal("raw", await KindOf(db, assetId));

            var asset = await db.ContentAssets.AsNoTracking().SingleAsync(a => a.Id == assetId);
            Assert.Equal(ContentAssetKind.Raw, asset.Kind);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<string> KindOf(SeoCopilotDbContext db, Guid assetId) =>
        await db.Database
            .SqlQueryRaw<string>("SELECT kind AS \"Value\" FROM content_assets WHERE id = {0}", assetId)
            .SingleAsync();
}
