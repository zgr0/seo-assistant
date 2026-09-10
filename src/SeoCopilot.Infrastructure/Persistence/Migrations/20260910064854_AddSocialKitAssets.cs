using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialKitAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "content_variants",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_alt",
                table: "content_variants",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "image_asset_id",
                table: "content_variants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_brief",
                table: "content_variants",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "content_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    bytes = table.Column<int>(type: "integer", nullable: false),
                    prompt = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_content_assets_content_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "content_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_content_assets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_content_variants_image_asset_id",
                table: "content_variants",
                column: "image_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_assets_job_id",
                table: "content_assets",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_assets_tenant_id",
                table: "content_assets",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_content_variants_content_assets_image_asset_id",
                table: "content_variants",
                column: "image_asset_id",
                principalTable: "content_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_content_variants_content_assets_image_asset_id",
                table: "content_variants");

            migrationBuilder.DropTable(
                name: "content_assets");

            migrationBuilder.DropIndex(
                name: "ix_content_variants_image_asset_id",
                table: "content_variants");

            migrationBuilder.DropColumn(
                name: "description",
                table: "content_variants");

            migrationBuilder.DropColumn(
                name: "image_alt",
                table: "content_variants");

            migrationBuilder.DropColumn(
                name: "image_asset_id",
                table: "content_variants");

            migrationBuilder.DropColumn(
                name: "image_brief",
                table: "content_variants");
        }
    }
}
