namespace TradingBot.Models;

public sealed class DailyTradingData
{
    public required DateOnly Date { get; init; }
    public required double Open { get; init; }
    public required double Close { get; init; }
    public required double High { get; init; }
    public required double Low { get; init; }
    public required double Volume { get; init; }
}
