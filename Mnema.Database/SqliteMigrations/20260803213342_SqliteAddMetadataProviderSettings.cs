#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class SqliteAddMetadataProviderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetadataProviderSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MetadataProvider = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesTitle = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesSummary = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesLocalizedName = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesCoverUrl = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesPublicationStatus = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesAgeRating = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesYear = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesTags = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesPeople = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesLinks = table.Column<bool>(type: "INTEGER", nullable: false),
                    Chapters = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChapterTitle = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChapterSummary = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChapterReleaseDate = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChapterPeople = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChapterTags = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChapterCoverUrl = table.Column<bool>(type: "INTEGER", nullable: false),
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
        }
    }
}
