using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sitecraft.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Marka profiline gorsel kimlik (renkler, logo) ve sosyal hesap adlari eklenir; varsayilan
    /// profil tek kaynaga iner. <c>sites.default_brand_profile_id</c> ile profildeki
    /// <c>is_default</c> ayni isi yapiyordu ve ikisi de uretimde okunmuyordu — kolon silinmeden
    /// once gosterdigi profil (o siteye ozelse) sitenin varsayilani yapilir. Kiraci geneli bir
    /// profili gosteriyorsa tasinmaz: yeni modelde "yalniz bu sitenin varsayilani olan kiraci
    /// geneli profil" ifade edilemez, kiraci varsayilani yapmak da diger siteleri etkilerdi.
    /// Ardindan kapsam basina fazladan varsayilan varsa en yenisi kalir ve tekil indeks kurulur.
    /// </summary>
    public partial class AddBrandVisualIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accent_color",
                table: "brand_profiles",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_storage_key",
                table: "brand_profiles",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_color",
                table: "brand_profiles",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "social_handles",
                table: "brand_profiles",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            // Sitede secili profil o siteye aitse kapsaminin tek varsayilani olur.
            migrationBuilder.Sql(
                """
                UPDATE brand_profiles b
                SET is_default = (b.id = s.default_brand_profile_id)
                FROM sites s
                WHERE b.site_id = s.id
                  AND b.tenant_id = s.tenant_id
                  AND EXISTS (
                      SELECT 1 FROM brand_profiles p
                      WHERE p.id = s.default_brand_profile_id
                        AND p.site_id = s.id
                        AND p.tenant_id = s.tenant_id);
                """);

            // Kapsam basina tek varsayilan — fazlasi varsa en yenisi kalir. PARTITION BY null
            // site_id'leri tek grupta toplar; kiraci geneli kapsam da boylece tekillesir.
            migrationBuilder.Sql(
                """
                UPDATE brand_profiles b
                SET is_default = false
                FROM (
                    SELECT id, row_number() OVER (
                        PARTITION BY tenant_id, site_id
                        ORDER BY created_at DESC, id DESC) AS rank
                    FROM brand_profiles
                    WHERE is_default
                ) d
                WHERE b.id = d.id AND d.rank > 1;
                """);

            migrationBuilder.DropColumn(
                name: "default_brand_profile_id",
                table: "sites");

            migrationBuilder.CreateIndex(
                name: "ix_brand_profiles_tenant_id_site_id",
                table: "brand_profiles",
                columns: new[] { "tenant_id", "site_id" },
                unique: true,
                filter: "is_default")
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_brand_profiles_tenant_id_site_id",
                table: "brand_profiles");

            migrationBuilder.AddColumn<Guid>(
                name: "default_brand_profile_id",
                table: "sites",
                type: "uuid",
                nullable: true);

            // Siteye ozel varsayilan eski kolona geri yazilir.
            migrationBuilder.Sql(
                """
                UPDATE sites s
                SET default_brand_profile_id = b.id
                FROM brand_profiles b
                WHERE b.site_id = s.id AND b.tenant_id = s.tenant_id AND b.is_default;
                """);

            migrationBuilder.DropColumn(
                name: "accent_color",
                table: "brand_profiles");

            migrationBuilder.DropColumn(
                name: "logo_storage_key",
                table: "brand_profiles");

            migrationBuilder.DropColumn(
                name: "primary_color",
                table: "brand_profiles");

            migrationBuilder.DropColumn(
                name: "social_handles",
                table: "brand_profiles");
        }
    }
}
