using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedReleases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessedContentReleases",
                table: "ProcessedContentReleases");

            migrationBuilder.DropIndex(
                name: "IX_ProcessedContentReleases_ReleaseId",
                table: "ProcessedContentReleases");

            migrationBuilder.RenameTable(
                name: "ProcessedContentReleases",
                newName: "ContentReleases");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "ContentReleases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContentReleases",
                table: "ContentReleases",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReleases_Type",
                table: "ContentReleases",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReleases_Type_ReleaseId",
                table: "ContentReleases",
                columns: new[] { "Type", "ReleaseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ContentReleases",
                table: "ContentReleases");

            migrationBuilder.DropIndex(
                name: "IX_ContentReleases_Type",
                table: "ContentReleases");

            migrationBuilder.DropIndex(
                name: "IX_ContentReleases_Type_ReleaseId",
                table: "ContentReleases");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ContentReleases");

            migrationBuilder.RenameTable(
                name: "ContentReleases",
                newName: "ProcessedContentReleases");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessedContentReleases",
                table: "ProcessedContentReleases",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedContentReleases_ReleaseId",
                table: "ProcessedContentReleases",
                column: "ReleaseId",
                unique: true);
        }
    }
}
