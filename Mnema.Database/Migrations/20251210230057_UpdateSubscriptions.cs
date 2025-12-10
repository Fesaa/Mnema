using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogEmptyDownloads",
                table: "UserPreferences");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Subscriptions",
                type: "TEXT",
                nullable: false,
                defaultValue: "{\"StartImmediately\":false,\"Extra\":{}}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Subscriptions");

            migrationBuilder.AddColumn<bool>(
                name: "LogEmptyDownloads",
                table: "UserPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
