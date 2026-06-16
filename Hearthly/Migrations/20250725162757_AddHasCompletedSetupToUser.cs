using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hearthly.Migrations
{
    /// <inheritdoc />
    public partial class AddHasCompletedSetupToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasCompletedSetup",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasCompletedSetup",
                table: "AspNetUsers");
        }
    }
}
