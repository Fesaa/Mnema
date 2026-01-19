using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoredSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DownloadUrl",
                table: "ContentReleases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "ContentReleases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MonitoredSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Providers = table.Column<int[]>(type: "integer[]", nullable: false),
                    BaseDir = table.Column<string>(type: "text", nullable: false),
                    ContentFormat = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    ValidTitles = table.Column<List<string>>(type: "text[]", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoredSeries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonitoredSeries");

            migrationBuilder.DropColumn(
                name: "DownloadUrl",
                table: "ContentReleases");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "ContentReleases");
        }
    }
}
