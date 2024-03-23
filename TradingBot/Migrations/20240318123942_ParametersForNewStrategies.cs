using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class ParametersForNewStrategies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuyLosersAnalysisLengthInDays",
                table: "StrategyParameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BuyLosersEvaluationFrequencyInDays",
                table: "StrategyParameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BuyWinnersAnalysisLengthInDays",
                table: "StrategyParameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BuyWinnersBuyWaitTimeInDays",
                table: "StrategyParameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BuyWinnersEvaluationFrequencyInDays",
                table: "StrategyParameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BuyWinnersSimultaneousEvaluations",
                table: "StrategyParameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "LimitPriceDamping",
                table: "StrategyParameters",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "PcaAnalysisLengthInDays",
                table: "StrategyParameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PcaDecompositionExpirationInDays",
                table: "StrategyParameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "PcaUndervaluedThreshold",
                table: "StrategyParameters",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PcaVarianceFraction",
                table: "StrategyParameters",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyLosersAnalysisLengthInDays",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "BuyLosersEvaluationFrequencyInDays",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "BuyWinnersAnalysisLengthInDays",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "BuyWinnersBuyWaitTimeInDays",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "BuyWinnersEvaluationFrequencyInDays",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "BuyWinnersSimultaneousEvaluations",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "LimitPriceDamping",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "PcaAnalysisLengthInDays",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "PcaDecompositionExpirationInDays",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "PcaUndervaluedThreshold",
                table: "StrategyParameters");

            migrationBuilder.DropColumn(
                name: "PcaVarianceFraction",
                table: "StrategyParameters");
        }
    }
}
