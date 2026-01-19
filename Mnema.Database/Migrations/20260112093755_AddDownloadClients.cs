using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DownloadClients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadClients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DownloadClients_Type",
                table: "DownloadClients",
                column: "Type",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DownloadClients");
        }
    }
}
