using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class SqliteAddImportScan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RootDir = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportScans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryImportResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportScanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Directory = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    QueuePosition = table.Column<int>(type: "INTEGER", nullable: false),
                    MonitoredSeriesId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParsedSeriesName = table.Column<string>(type: "TEXT", nullable: false),
                    ParsedHardcoverId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParsedMangaBakaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Files = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryImportResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectoryImportResults_ImportScans_ImportScanId",
                        column: x => x.ImportScanId,
                        principalTable: "ImportScans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DirectoryImportResults_MonitoredSeries_MonitoredSeriesId",
                        column: x => x.MonitoredSeriesId,
                        principalTable: "MonitoredSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ImportErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    StackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImportScanId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportErrors_ImportScans_ImportScanId",
                        column: x => x.ImportScanId,
                        principalTable: "ImportScans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryImportResults_ImportScanId",
                table: "DirectoryImportResults",
                column: "ImportScanId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryImportResults_MonitoredSeriesId",
                table: "DirectoryImportResults",
                column: "MonitoredSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportErrors_ImportScanId",
                table: "ImportErrors",
                column: "ImportScanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectoryImportResults");

            migrationBuilder.DropTable(
                name: "ImportErrors");

            migrationBuilder.DropTable(
                name: "ImportScans");
        }
    }
}
