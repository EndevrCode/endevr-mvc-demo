using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Hearthly.Migrations
{
    /// <inheritdoc />
    public partial class AddNSRIToEmergencyContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EmergencyContacts",
                columns: new[] { "Id", "ContactType", "FamilyId", "Name", "Notes", "PhoneNumber" },
                values: new object[,]
                {
                    { 1, 0, null, "Local Police", null, "10111" },
                    { 2, 1, null, "Fire Station", null, "10177" },
                    { 3, 2, null, "Ambulance Service", null, "10177" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EmergencyContacts",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "EmergencyContacts",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "EmergencyContacts",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
