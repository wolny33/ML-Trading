using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedTradingActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AlpacaId",
                table: "TradingActions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AverageFillPrice",
                table: "TradingActions",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExecutionTimestamp",
                table: "TradingActions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TradingActions",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlpacaId",
                table: "TradingActions");

            migrationBuilder.DropColumn(
                name: "AverageFillPrice",
                table: "TradingActions");

            migrationBuilder.DropColumn(
                name: "ExecutionTimestamp",
                table: "TradingActions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TradingActions");
        }
    }
}
