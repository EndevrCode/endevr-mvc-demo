using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nestled.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDoneAndDurationToRuleOfThreeTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDone",
                table: "RuleOfThreeTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDone",
                table: "RuleOfThreeTasks");
        }
    }
}
