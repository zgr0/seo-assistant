using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// <c>AddCaptionedImageAssets</c> kind kolonunu <c>''</c> varsayilaniyla ekledi; ondan once
    /// uretilen gorsellerin kind degeri bos kaldi ve enum cozumlenemedigi icin bu gorselleri
    /// iceren her sorgu (galeri, is detayi, is listesi) 500 donuyordu. O donemde yazi basma
    /// yoktu, dolayisiyla eski satirlarin hepsi ham gorseldir.
    /// </summary>
    public partial class BackfillContentAssetKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE content_assets SET kind = 'raw' WHERE kind = '';");

            // Model degismedi (EF degeri her zaman yazar); varsayilan yalniz elle yazilan
            // SQL'in ayni hataya dusmemesi icin.
            migrationBuilder.Sql("ALTER TABLE content_assets ALTER COLUMN kind SET DEFAULT 'raw';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE content_assets ALTER COLUMN kind SET DEFAULT '';");
        }
    }
}
