#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class SqliteAddNamingFormats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChapterFileFormat",
                table: "Preferences",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OneShotFileFormat",
                table: "Preferences",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChapterFileFormat",
                table: "Preferences");

            migrationBuilder.DropColumn(
                name: "OneShotFileFormat",
                table: "Preferences");
        }
    }
}
