using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class AssetsStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetsStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreationTimestamp = table.Column<long>(type: "INTEGER", nullable: false),
                    MainCurrency = table.Column<string>(type: "TEXT", nullable: false),
                    EquityValue = table.Column<double>(type: "REAL", nullable: false),
                    AvailableCash = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetsStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SymbolId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<double>(type: "REAL", nullable: false),
                    AvailableQuantity = table.Column<double>(type: "REAL", nullable: false),
                    MarketValue = table.Column<double>(type: "REAL", nullable: false),
                    AverageEntryPrice = table.Column<double>(type: "REAL", nullable: false),
                    AssetsStateId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_AssetsStates_AssetsStateId",
                        column: x => x.AssetsStateId,
                        principalTable: "AssetsStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetsStates_CreationTimestamp",
                table: "AssetsStates",
                column: "CreationTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_AssetsStateId",
                table: "Positions",
                column: "AssetsStateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "AssetsStates");
        }
    }
}
