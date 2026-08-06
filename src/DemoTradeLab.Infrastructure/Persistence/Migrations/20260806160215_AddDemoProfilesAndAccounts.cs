using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoTradeLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoProfilesAndAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemoAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemoProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TotalBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    ReservedBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoAccounts_DemoProfiles_DemoProfileId",
                        column: x => x.DemoProfileId,
                        principalTable: "DemoProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoAccounts_DemoProfileId_Key",
                table: "DemoAccounts",
                columns: new[] { "DemoProfileId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoProfiles_Key",
                table: "DemoProfiles",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoAccounts");

            migrationBuilder.DropTable(
                name: "DemoProfiles");
        }
    }
}
