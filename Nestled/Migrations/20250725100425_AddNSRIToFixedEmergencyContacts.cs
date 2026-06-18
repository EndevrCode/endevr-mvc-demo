using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nestled.Migrations
{
    /// <inheritdoc />
    public partial class AddNSRIToFixedEmergencyContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EmergencyContacts",
                columns: new[] { "Id", "ContactType", "FamilyId", "Name", "Notes", "PhoneNumber" },
                values: new object[] { 4, 5, null, "Coast Guard", null, "10117" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EmergencyContacts",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
