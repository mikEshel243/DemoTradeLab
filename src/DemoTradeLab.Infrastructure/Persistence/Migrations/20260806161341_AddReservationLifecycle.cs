using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoTradeLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemoAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoReservations_DemoAccounts_DemoAccountId",
                        column: x => x.DemoAccountId,
                        principalTable: "DemoAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoReservations_DemoAccountId",
                table: "DemoReservations",
                column: "DemoAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DemoReservations_DemoAccountId_CreatedAtUtc",
                table: "DemoReservations",
                columns: new[] { "DemoAccountId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoReservations");
        }
    }
}
