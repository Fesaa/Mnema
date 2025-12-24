using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePageEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Modifiers",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Providers",
                table: "Pages");

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "Pages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Pages");

            migrationBuilder.AddColumn<string>(
                name: "Modifiers",
                table: "Pages",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int[]>(
                name: "Providers",
                table: "Pages",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);
        }
    }
}
