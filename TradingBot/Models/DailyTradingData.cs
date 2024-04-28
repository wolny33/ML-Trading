namespace TradingBot.Models;

public sealed class DailyTradingData
{
    public required DateOnly Date { get; init; }
    public required decimal Open { get; init; }
    public required decimal Close { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Volume { get; init; }
    public required decimal FearGreedIndex { get; init;}
}
