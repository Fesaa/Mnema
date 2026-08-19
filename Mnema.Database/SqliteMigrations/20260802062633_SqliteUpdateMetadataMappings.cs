#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class SqliteUpdateMetadataMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetadataFieldMappings",
                table: "Preferences",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetadataFieldMappings",
                table: "Preferences");
        }
    }
}
