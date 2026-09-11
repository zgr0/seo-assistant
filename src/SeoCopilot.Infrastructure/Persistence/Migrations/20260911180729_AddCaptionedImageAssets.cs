using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptionedImageAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kind",
                table: "content_assets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "source_asset_id",
                table: "content_assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_content_assets_source_asset_id",
                table: "content_assets",
                column: "source_asset_id");

            migrationBuilder.AddForeignKey(
                name: "fk_content_assets_content_assets_source_asset_id",
                table: "content_assets",
                column: "source_asset_id",
                principalTable: "content_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_content_assets_content_assets_source_asset_id",
                table: "content_assets");

            migrationBuilder.DropIndex(
                name: "ix_content_assets_source_asset_id",
                table: "content_assets");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "content_assets");

            migrationBuilder.DropColumn(
                name: "source_asset_id",
                table: "content_assets");
        }
    }
}
