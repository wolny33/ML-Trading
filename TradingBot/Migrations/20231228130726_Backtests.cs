using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class Backtests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BacktestId",
                table: "TradingTasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BacktestId",
                table: "AssetsStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Backtests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SimulationStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SimulationEnd = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExecutionStartTimestamp = table.Column<long>(type: "INTEGER", nullable: false),
                    ExecutionEndTimestamp = table.Column<long>(type: "INTEGER", nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    StateDetails = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Backtests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradingTasks_BacktestId",
                table: "TradingTasks",
                column: "BacktestId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetsStates_BacktestId",
                table: "AssetsStates",
                column: "BacktestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetsStates_Backtests_BacktestId",
                table: "AssetsStates",
                column: "BacktestId",
                principalTable: "Backtests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TradingTasks_Backtests_BacktestId",
                table: "TradingTasks",
                column: "BacktestId",
                principalTable: "Backtests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetsStates_Backtests_BacktestId",
                table: "AssetsStates");

            migrationBuilder.DropForeignKey(
                name: "FK_TradingTasks_Backtests_BacktestId",
                table: "TradingTasks");

            migrationBuilder.DropTable(
                name: "Backtests");

            migrationBuilder.DropIndex(
                name: "IX_TradingTasks_BacktestId",
                table: "TradingTasks");

            migrationBuilder.DropIndex(
                name: "IX_AssetsStates_BacktestId",
                table: "AssetsStates");

            migrationBuilder.DropColumn(
                name: "BacktestId",
                table: "TradingTasks");

            migrationBuilder.DropColumn(
                name: "BacktestId",
                table: "AssetsStates");
        }
    }
}
