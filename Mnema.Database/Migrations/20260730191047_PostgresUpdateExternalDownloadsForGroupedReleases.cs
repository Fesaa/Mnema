using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class PostgresUpdateExternalDownloadsForGroupedReleases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsErrored",
                table: "ExternalDownloads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ExternalDownloads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsGroupedRelease",
                table: "ContentReleases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<List<string>>(
                name: "Roles",
                table: "AuthKeys",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>(),
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldDefaultValue: new List<string>());

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDownloads_ExternalId",
                table: "ExternalDownloads",
                column: "ExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExternalDownloads_ExternalId",
                table: "ExternalDownloads");

            migrationBuilder.DropColumn(
                name: "IsErrored",
                table: "ExternalDownloads");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ExternalDownloads");

            migrationBuilder.DropColumn(
                name: "IsGroupedRelease",
                table: "ContentReleases");

            migrationBuilder.AlterColumn<List<string>>(
                name: "Roles",
                table: "AuthKeys",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>(),
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldDefaultValue: new List<string>());
        }
    }
}
