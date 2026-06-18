using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nestled.Migrations
{
    /// <inheritdoc />
    public partial class AmbulanceEmergencyContactUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EmergencyContacts",
                keyColumn: "Id",
                keyValue: 3,
                column: "PhoneNumber",
                value: "082911");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EmergencyContacts",
                keyColumn: "Id",
                keyValue: 3,
                column: "PhoneNumber",
                value: "10177");
        }
    }
}
