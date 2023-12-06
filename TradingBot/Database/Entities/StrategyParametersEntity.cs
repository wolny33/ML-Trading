using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities
{
    public class StrategyParametersEntity
    {
        [Key]
        public required Guid Id { get; init; }

        public required int MaxStocksBuyCount { get; set; }
        public required int MinDaysDecreasing { get; set; }
        public required double TopGrowingSymbolsBuyRatio { get; set; }
    }
}
