using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class PcaStrategyChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PcaDiverseThreshold",
                table: "StrategyParameters",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PcaIgnoredThreshold",
                table: "StrategyParameters",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "L1Norms",
                table: "PcaDecompositions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "L2Norms",
                table: "PcaDecompositions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "MeanPredictorError",
                table: "Backtests",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PcaDiverseThreshold",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "PcaIgnoredThreshold",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "L1Norms",
                table: "PcaDecompositions");

            migrationBuilder.DropColumn(
                name: "L2Norms",
                table: "PcaDecompositions");

            migrationBuilder.DropColumn(
                name: "MeanPredictorError",
                table: "Backtests");
        }
    }
}
