using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoTradeLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderRecoveryWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemoAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReservationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoOrders_DemoAccounts_DemoAccountId",
                        column: x => x.DemoAccountId,
                        principalTable: "DemoAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemoOrders_DemoReservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "DemoReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationCompletionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemoAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReservationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationCompletionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationCompletionRecords_DemoAccounts_DemoAccountId",
                        column: x => x.DemoAccountId,
                        principalTable: "DemoAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationCompletionRecords_DemoReservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "DemoReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemoOrderEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoOrderEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoOrderEvents_DemoOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "DemoOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoOrderEvents_OrderId_OccurredAtUtc",
                table: "DemoOrderEvents",
                columns: new[] { "OrderId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DemoOrders_DemoAccountId",
                table: "DemoOrders",
                column: "DemoAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DemoOrders_ReservationId",
                table: "DemoOrders",
                column: "ReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationCompletionRecords_DemoAccountId_Key",
                table: "ReservationCompletionRecords",
                columns: new[] { "DemoAccountId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationCompletionRecords_ReservationId",
                table: "ReservationCompletionRecords",
                column: "ReservationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoOrderEvents");

            migrationBuilder.DropTable(
                name: "ReservationCompletionRecords");

            migrationBuilder.DropTable(
                name: "DemoOrders");
        }
    }
}
