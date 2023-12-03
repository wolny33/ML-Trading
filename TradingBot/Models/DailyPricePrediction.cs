namespace TradingBot.Models;

public sealed class DailyPricePrediction
{
    public required DateOnly Date { get; init; }
    public required decimal ClosingPrice { get; init; }
    public required decimal HighPrice { get; init; }
    public required decimal LowPrice { get; init; }
}
