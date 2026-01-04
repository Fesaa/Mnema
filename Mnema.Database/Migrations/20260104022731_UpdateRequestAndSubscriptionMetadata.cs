using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRequestAndSubscriptionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "Subscriptions",
                type: "TEXT",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldDefaultValue: "{\"StartImmediately\":false,\"Extra\":{}}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "Subscriptions",
                type: "TEXT",
                nullable: false,
                defaultValue: "{\"StartImmediately\":false,\"Extra\":{}}",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldDefaultValue: "{}");
        }
    }
}
