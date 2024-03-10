using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class StrategyStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuyLosersStrategyStates",
                columns: table => new
                {
                    BacktestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NextEvaluationDay = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuyLosersStrategyStates", x => x.BacktestId);
                });

            migrationBuilder.CreateTable(
                name: "BuyWinnersStrategyStates",
                columns: table => new
                {
                    BacktestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NextEvaluationDay = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuyWinnersStrategyStates", x => x.BacktestId);
                });

            migrationBuilder.CreateTable(
                name: "LoserSymbolsToBuy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StrategyStateBacktestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoserSymbolsToBuy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoserSymbolsToBuy_BuyLosersStrategyStates_StrategyStateBacktestId",
                        column: x => x.StrategyStateBacktestId,
                        principalTable: "BuyLosersStrategyStates",
                        principalColumn: "BacktestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuyWinnersEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StrategyStateBacktestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Bought = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuyWinnersEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuyWinnersEvaluations_BuyWinnersStrategyStates_StrategyStateBacktestId",
                        column: x => x.StrategyStateBacktestId,
                        principalTable: "BuyWinnersStrategyStates",
                        principalColumn: "BacktestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WinnerBuyActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WinnerBuyActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WinnerBuyActions_BuyWinnersEvaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "BuyWinnersEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WinnerSymbolsToBuy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WinnerSymbolsToBuy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WinnerSymbolsToBuy_BuyWinnersEvaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "BuyWinnersEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuyWinnersEvaluations_StrategyStateBacktestId",
                table: "BuyWinnersEvaluations",
                column: "StrategyStateBacktestId");

            migrationBuilder.CreateIndex(
                name: "IX_LoserSymbolsToBuy_StrategyStateBacktestId",
                table: "LoserSymbolsToBuy",
                column: "StrategyStateBacktestId");

            migrationBuilder.CreateIndex(
                name: "IX_WinnerBuyActions_EvaluationId",
                table: "WinnerBuyActions",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_WinnerSymbolsToBuy_EvaluationId",
                table: "WinnerSymbolsToBuy",
                column: "EvaluationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoserSymbolsToBuy");

            migrationBuilder.DropTable(
                name: "WinnerBuyActions");

            migrationBuilder.DropTable(
                name: "WinnerSymbolsToBuy");

            migrationBuilder.DropTable(
                name: "BuyLosersStrategyStates");

            migrationBuilder.DropTable(
                name: "BuyWinnersEvaluations");

            migrationBuilder.DropTable(
                name: "BuyWinnersStrategyStates");
        }
    }
}
