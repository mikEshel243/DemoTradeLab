using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoTradeLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Instrument = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    OpeningPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    ClosingPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    RealizedProfitLoss = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Fees = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: true),
                    FinancingCosts = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ImportedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_ClosedAtUtc",
                table: "Trades",
                column: "ClosedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_Instrument",
                table: "Trades",
                column: "Instrument");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trades");
        }
    }
}
