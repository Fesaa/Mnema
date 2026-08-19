#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mnema.Database.SqliteMigrations
{
    /// <inheritdoc />
    public partial class SqliteRemoveUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthKeys_Users_UserId",
                table: "AuthKeys");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Users_UserId",
                table: "Subscriptions");

            migrationBuilder.DropTable(
                name: "MnemaUserPage");

            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImageFormat = table.Column<int>(type: "INTEGER", nullable: false),
                    CoverFallbackMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    ConvertToGenreList = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    BlackListedTags = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    WhiteListedTags = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    PinSubscriptionTitles = table.Column<bool>(type: "INTEGER", nullable: false),
                    AgeRatingMappings = table.Column<string>(type: "TEXT", nullable: false),
                    TagMappings = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferences", x => x.Id);
                });

            migrationBuilder.Sql(@"
                INSERT INTO Preferences (Id, ImageFormat, CoverFallbackMethod, ConvertToGenreList,
                                          BlackListedTags, WhiteListedTags, PinSubscriptionTitles,
                                          AgeRatingMappings, TagMappings)
                SELECT Id, ImageFormat, CoverFallbackMethod, ConvertToGenreList,
                       BlackListedTags, WhiteListedTags, PinSubscriptionTitles,
                       AgeRatingMappings, TagMappings
                FROM UserPreferences
                LIMIT 1;
            ");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_AuthKeys_UserId",
                table: "AuthKeys");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MonitoredSeries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ExternalDownloads");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AuthKeys");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Preferences");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Subscriptions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Notifications",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "MonitoredSeries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ExternalDownloads",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "AuthKeys",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MnemaUserPage",
                columns: table => new
                {
                    PagesId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsersId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MnemaUserPage", x => new { x.PagesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_MnemaUserPage_Pages_PagesId",
                        column: x => x.PagesId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MnemaUserPage_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BlackListedTags = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    ConvertToGenreList = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    CoverFallbackMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageFormat = table.Column<int>(type: "INTEGER", nullable: false),
                    PinSubscriptionTitles = table.Column<bool>(type: "INTEGER", nullable: false),
                    WhiteListedTags = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    AgeRatingMappings = table.Column<string>(type: "TEXT", nullable: false),
                    TagMappings = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthKeys_UserId",
                table: "AuthKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MnemaUserPage_UsersId",
                table: "MnemaUserPage",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthKeys_Users_UserId",
                table: "AuthKeys",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Users_UserId",
                table: "Subscriptions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
