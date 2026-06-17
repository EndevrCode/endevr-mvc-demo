using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hearthly.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFamilyLocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatteryLevel",
                table: "FamilyLocations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCharging",
                table: "FamilyLocations",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaceName",
                table: "FamilyLocations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Speed",
                table: "FamilyLocations",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BatteryLevel", table: "FamilyLocations");
            migrationBuilder.DropColumn(name: "IsCharging",   table: "FamilyLocations");
            migrationBuilder.DropColumn(name: "PlaceName",    table: "FamilyLocations");
            migrationBuilder.DropColumn(name: "Speed",        table: "FamilyLocations");
        }
    }
}
