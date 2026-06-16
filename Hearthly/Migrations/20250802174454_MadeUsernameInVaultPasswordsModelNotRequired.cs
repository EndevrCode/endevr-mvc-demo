using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hearthly.Migrations
{
    /// <inheritdoc />
    public partial class MadeUsernameInVaultPasswordsModelNotRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaultPasswords_AspNetUsers_UserId",
                table: "VaultPasswords");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "VaultPasswords",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_VaultPasswords_AspNetUsers_UserId",
                table: "VaultPasswords",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaultPasswords_AspNetUsers_UserId",
                table: "VaultPasswords");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "VaultPasswords",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VaultPasswords_AspNetUsers_UserId",
                table: "VaultPasswords",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
