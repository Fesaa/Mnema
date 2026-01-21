using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mnema.Database.Migrations
{
    /// <inheritdoc />
    public partial class LimitMonitoredSeriesToOneProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "MonitoredSeries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Migrate data: extract the first value from Providers array to Provider column
            migrationBuilder.Sql(@"
                    UPDATE ""MonitoredSeries""
                    SET ""Provider"" = ""Providers""[1]
                    WHERE ""Providers"" IS NOT NULL
                    AND array_length(""Providers"", 1) > 0;
                ");


            migrationBuilder.DropColumn(
                name: "Providers",
                table: "MonitoredSeries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Provider",
                table: "MonitoredSeries");

            migrationBuilder.AddColumn<int[]>(
                name: "Providers",
                table: "MonitoredSeries",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);
        }
    }
}
