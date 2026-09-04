using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRedirectAndSchemaValidityRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "rules",
                columns: new[] { "code", "category", "description_tr", "doc_url", "how_to_fix_tr", "is_active", "severity", "title_tr", "weight" },
                values: new object[,]
                {
                    { "INVALID_STRUCTURED_DATA", "structured_data", "Sayfada JSON-LD var ama bicimi bozuk; arama motorlari okuyamaz.", null, "Her kok nesneyi ayri bir <script type=\"application/ld+json\"> icine koy, JSON'u dogrula.", true, "medium", "Yapisal veri gecersiz", 4 },
                    { "REDIRECT_TARGET_INVALID", "indexability", "Yonlendirme http/https disi bir adrese gidiyor; hicbir istemci hedefe ulasamaz.", null, "Location basligini gercek bir URL'e cevir ya da yonlendirmeyi kaldir.", true, "critical", "Yonlendirme hedefi gecersiz", 9 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INVALID_STRUCTURED_DATA");

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_TARGET_INVALID");
        }
    }
}
