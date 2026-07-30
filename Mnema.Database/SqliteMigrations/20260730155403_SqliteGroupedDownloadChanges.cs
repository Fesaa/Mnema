using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class SqliteGroupedDownloadChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ExternalDownloads",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsGroupedRelease",
                table: "ContentReleases",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "ExternalDownloads");

            migrationBuilder.DropColumn(
                name: "IsGroupedRelease",
                table: "ContentReleases");
        }
    }
}
