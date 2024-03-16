using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class PcaDecompositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PcaDecompositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BacktestId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreationTimestamp = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Symbols = table.Column<string>(type: "TEXT", nullable: false),
                    Means = table.Column<string>(type: "TEXT", nullable: false),
                    StandardDeviations = table.Column<string>(type: "TEXT", nullable: false),
                    PrincipalVectors = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PcaDecompositions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PcaDecompositions_CreationTimestamp",
                table: "PcaDecompositions",
                column: "CreationTimestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PcaDecompositions");
        }
    }
}
