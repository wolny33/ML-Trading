namespace TradingBot.Models;

public sealed class DailyPricePrediction
{
    public required DateOnly Date { get; init; }
    public required double ClosingPrice { get; init; }
    public required double HighPrice { get; init; }
    public required double LowPrice { get; init; }
}
