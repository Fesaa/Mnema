using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class RemoveSearchTitlesUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchTitles_NormalizedTitle",
                table: "SearchTitles");

            migrationBuilder.CreateIndex(
                name: "IX_SearchTitles_NormalizedTitle",
                table: "SearchTitles",
                column: "NormalizedTitle");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchTitles_NormalizedTitle",
                table: "SearchTitles");

            migrationBuilder.CreateIndex(
                name: "IX_SearchTitles_NormalizedTitle",
                table: "SearchTitles",
                column: "NormalizedTitle",
                unique: true);
        }
    }
}
