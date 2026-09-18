using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class AddSearchTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedTitle = table.Column<string>(type: "TEXT", nullable: false),
                    MonitoredSeriesId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchTitles_MonitoredSeries_MonitoredSeriesId",
                        column: x => x.MonitoredSeriesId,
                        principalTable: "MonitoredSeries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchTitles_MonitoredSeriesId",
                table: "SearchTitles",
                column: "MonitoredSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchTitles_NormalizedTitle",
                table: "SearchTitles",
                column: "NormalizedTitle",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchTitles");
        }
    }
}
