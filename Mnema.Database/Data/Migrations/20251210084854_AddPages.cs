using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    SortValue = table.Column<int>(type: "integer", nullable: false),
                    Providers = table.Column<int[]>(type: "integer[]", nullable: false),
                    Dirs = table.Column<string[]>(type: "text[]", nullable: false),
                    CustomRootDir = table.Column<string>(type: "text", nullable: false),
                    Modifiers = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pages");
        }
    }
}
