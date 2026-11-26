using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class SqliteAddMangaBakaCollectionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MangaBakaCollectionId",
                table: "MonitoredSeries",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MangaBakaCollectionId",
                table: "MonitoredSeries");
        }
    }
}
