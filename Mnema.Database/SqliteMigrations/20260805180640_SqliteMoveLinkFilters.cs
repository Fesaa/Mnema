using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class SqliteMoveLinkFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkFilters",
                table: "Preferences",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkFilters",
                table: "Preferences");
        }
    }
}
