using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hearthly.Migrations
{
    /// <inheritdoc />
    public partial class AddVaultPinHashToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VaultPinHash",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VaultPinHash",
                table: "AspNetUsers");
        }
    }
}
