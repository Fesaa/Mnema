using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class PostgresAddMetadataProviderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<string>>(
                name: "Roles",
                table: "AuthKeys",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>(),
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldDefaultValue: new List<string>());

            migrationBuilder.CreateTable(
                name: "MetadataProviderSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetadataProvider = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesTitle = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesSummary = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesLocalizedName = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesCoverUrl = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesPublicationStatus = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesAgeRating = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesYear = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesTags = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesPeople = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesLinks = table.Column<bool>(type: "boolean", nullable: false),
                    Chapters = table.Column<bool>(type: "boolean", nullable: false),
                    ChapterTitle = table.Column<bool>(type: "boolean", nullable: false),
                    ChapterSummary = table.Column<bool>(type: "boolean", nullable: false),
                    ChapterReleaseDate = table.Column<bool>(type: "boolean", nullable: false),
                    ChapterPeople = table.Column<bool>(type: "boolean", nullable: false),
                    ChapterTags = table.Column<bool>(type: "boolean", nullable: false),
                    ChapterCoverUrl = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataProviderSpecific = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataProviderSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataProviderSettings_MetadataProvider",
                table: "MetadataProviderSettings",
                column: "MetadataProvider",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetadataProviderSettings");

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
