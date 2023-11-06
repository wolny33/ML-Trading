namespace TradingBot.Models;

public sealed class Prediction
{
    public required IReadOnlyList<DailyPriceInfo> Prices { get; init; }
}
