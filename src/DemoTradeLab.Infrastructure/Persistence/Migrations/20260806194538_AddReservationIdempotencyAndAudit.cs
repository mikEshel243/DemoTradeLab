using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoTradeLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationIdempotencyAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemoAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReservationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationAuditEntries_DemoAccounts_DemoAccountId",
                        column: x => x.DemoAccountId,
                        principalTable: "DemoAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationAuditEntries_DemoReservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "DemoReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationIdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemoAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ReservationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationIdempotencyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationIdempotencyRecords_DemoAccounts_DemoAccountId",
                        column: x => x.DemoAccountId,
                        principalTable: "DemoAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationIdempotencyRecords_DemoReservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "DemoReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAuditEntries_DemoAccountId_OccurredAtUtc",
                table: "ReservationAuditEntries",
                columns: new[] { "DemoAccountId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAuditEntries_ReservationId",
                table: "ReservationAuditEntries",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationIdempotencyRecords_DemoAccountId_Key",
                table: "ReservationIdempotencyRecords",
                columns: new[] { "DemoAccountId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationIdempotencyRecords_ReservationId",
                table: "ReservationIdempotencyRecords",
                column: "ReservationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationAuditEntries");

            migrationBuilder.DropTable(
                name: "ReservationIdempotencyRecords");
        }
    }
}
