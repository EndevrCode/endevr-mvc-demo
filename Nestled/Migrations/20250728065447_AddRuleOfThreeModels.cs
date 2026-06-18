using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nestled.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleOfThreeModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowGuardianAccess",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "GuardianAccessDisabledAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RuleOfThreeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsFamilyEntry = table.Column<bool>(type: "bit", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MainProject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsedTimers = table.Column<int>(type: "int", nullable: false),
                    IsPowerDay = table.Column<bool>(type: "bit", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    StreakAtDay = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleOfThreeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleOfThreeEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleOfThreeTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleOfThreeTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleOfThreeTasks_RuleOfThreeEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "RuleOfThreeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleOfThreeEntries_UserId",
                table: "RuleOfThreeEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleOfThreeTasks_EntryId",
                table: "RuleOfThreeTasks",
                column: "EntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuleOfThreeTasks");

            migrationBuilder.DropTable(
                name: "RuleOfThreeEntries");

            migrationBuilder.DropColumn(
                name: "AllowGuardianAccess",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GuardianAccessDisabledAt",
                table: "AspNetUsers");
        }
    }
}
