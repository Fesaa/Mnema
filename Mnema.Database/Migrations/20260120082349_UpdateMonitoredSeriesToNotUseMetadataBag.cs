using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMonitoredSeriesToNotUseMetadataBag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "MonitoredSeries");

            migrationBuilder.AddColumn<string>(
                name: "HardcoverId",
                table: "MonitoredSeries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MangaBakaId",
                table: "MonitoredSeries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleOverride",
                table: "MonitoredSeries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HardcoverId",
                table: "MonitoredSeries");

            migrationBuilder.DropColumn(
                name: "MangaBakaId",
                table: "MonitoredSeries");

            migrationBuilder.DropColumn(
                name: "TitleOverride",
                table: "MonitoredSeries");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "MonitoredSeries",
                type: "TEXT",
                nullable: false,
                defaultValue: "{}");
        }
    }
}
