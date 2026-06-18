using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nestled.Migrations
{
    /// <inheritdoc />
    public partial class AddTowingServicesToEmergencyContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EmergencyContacts",
                columns: new[] { "Id", "ContactType", "FamilyId", "Name", "Notes", "PhoneNumber" },
                values: new object[] { 5, 6, null, "AAA Towing Service", null, "0119433538" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EmergencyContacts",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
