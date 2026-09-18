using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class AddDroppedContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchTitles_MonitoredSeries_MonitoredSeriesId",
                table: "SearchTitles");

            migrationBuilder.AlterColumn<Guid>(
                name: "MonitoredSeriesId",
                table: "SearchTitles",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DroppedContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MonitoredSeriesId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Files = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DroppedContent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DroppedContent_MonitoredSeries_MonitoredSeriesId",
                        column: x => x.MonitoredSeriesId,
                        principalTable: "MonitoredSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DroppedContent_MonitoredSeriesId",
                table: "DroppedContent",
                column: "MonitoredSeriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchTitles_MonitoredSeries_MonitoredSeriesId",
                table: "SearchTitles",
                column: "MonitoredSeriesId",
                principalTable: "MonitoredSeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchTitles_MonitoredSeries_MonitoredSeriesId",
                table: "SearchTitles");

            migrationBuilder.DropTable(
                name: "DroppedContent");

            migrationBuilder.AlterColumn<Guid>(
                name: "MonitoredSeriesId",
                table: "SearchTitles",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchTitles_MonitoredSeries_MonitoredSeriesId",
                table: "SearchTitles",
                column: "MonitoredSeriesId",
                principalTable: "MonitoredSeries",
                principalColumn: "Id");
        }
    }
}
