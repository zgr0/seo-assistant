using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Sayfa gorsel adresleri. Varsayilan <c>'{}'</c> sart: dolu bir tabloya varsayilansiz
    /// NOT NULL kolon eklemek Postgres'te basarisiz olur ve uygulama acilmaz. Eski taramalarin
    /// gorsel adresi yoktur — site gorseli icin yeniden tarama gerekir, o zamana kadar kart kullanilir.
    /// </summary>
    public partial class AddPageImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "image_urls",
                table: "pages",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_urls",
                table: "pages");
        }
    }
}
