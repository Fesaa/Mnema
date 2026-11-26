using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class PostgresAddMangaBakaCollectionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MangaBakaCollectionId",
                table: "MonitoredSeries",
                type: "text",
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
