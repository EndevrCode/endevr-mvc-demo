using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hearthly.Migrations
{
    /// <inheritdoc />
    public partial class AddPetEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Species = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Breed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhotoPath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HasInsurance = table.Column<bool>(type: "bit", nullable: false),
                    InsuranceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VetContact = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsMicrochipped = table.Column<bool>(type: "bit", nullable: false),
                    MicrochipNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastDewormingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTickFleaDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastGroomingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCheckupDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeceased = table.Column<bool>(type: "bit", nullable: false),
                    DateOfDeath = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pets_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pets_FamilyId",
                table: "Pets",
                column: "FamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pets");
        }
    }
}
