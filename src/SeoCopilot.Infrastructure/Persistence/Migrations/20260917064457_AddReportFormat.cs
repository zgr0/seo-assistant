using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Raporlara cikti bicimi eklenir. Bu migration'dan onceki butun raporlar HTML olarak
    /// uretildi, o yuzden varsayilan <c>html</c>: EF'in urettigi <c>''</c> varsayilani birakilsa
    /// enum cozumlenemez ve eski raporlari donduren her sorgu 500 dogururdu
    /// (bkz. <c>BackfillContentAssetKind</c>). Yeni kayitlarda degeri EF yazar; kolon
    /// varsayilani yalniz elle yazilan SQL icin guvenlik agidir.
    /// </summary>
    public partial class AddReportFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "format",
                table: "reports",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "html");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "format",
                table: "reports");
        }
    }
}
