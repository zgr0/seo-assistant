using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSiteVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verification_method",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "verification_token",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "sites");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "verification_method",
                table: "sites",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "verification_token",
                table: "sites",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "sites",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
