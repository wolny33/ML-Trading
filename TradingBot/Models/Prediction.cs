namespace TradingBot.Models;

public sealed class Prediction
{
    public required IReadOnlyList<DailyPricePrediction> Prices { get; init; }
}
