using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class PostgresMigrateLinkFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkFilters",
                table: "Preferences",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkFilters",
                table: "Preferences");

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
