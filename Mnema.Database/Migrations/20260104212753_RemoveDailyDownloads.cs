using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDailyDownloads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRun",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LastRunSuccess",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "NextRun",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RefreshFrequency",
                table: "Subscriptions");

            migrationBuilder.RenameColumn(
                name: "NoDownloadsRuns",
                table: "Subscriptions",
                newName: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Subscriptions",
                newName: "NoDownloadsRuns");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRun",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "LastRunSuccess",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRun",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RefreshFrequency",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 2);
        }
    }
}
