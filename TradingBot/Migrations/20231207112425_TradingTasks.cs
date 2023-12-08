using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class TradingTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Details");

            migrationBuilder.AddColumn<Guid>(
                name: "TradingTaskId",
                table: "TradingActions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TradingTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartTimestamp = table.Column<long>(type: "INTEGER", nullable: false),
                    EndTimestamp = table.Column<long>(type: "INTEGER", nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    StateDetails = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingTasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradingActions_TradingTaskId",
                table: "TradingActions",
                column: "TradingTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_TradingActions_TradingTasks_TradingTaskId",
                table: "TradingActions",
                column: "TradingTaskId",
                principalTable: "TradingTasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradingActions_TradingTasks_TradingTaskId",
                table: "TradingActions");

            migrationBuilder.DropTable(
                name: "TradingTasks");

            migrationBuilder.DropIndex(
                name: "IX_TradingActions_TradingTaskId",
                table: "TradingActions");

            migrationBuilder.DropColumn(
                name: "TradingTaskId",
                table: "TradingActions");

            migrationBuilder.CreateTable(
                name: "Details",
                columns: table => new
                {
                    TradingActionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Details", x => x.TradingActionId);
                    table.ForeignKey(
                        name: "FK_Details_TradingActions_TradingActionId",
                        column: x => x.TradingActionId,
                        principalTable: "TradingActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
