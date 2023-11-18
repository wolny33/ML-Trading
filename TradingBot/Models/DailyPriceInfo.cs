namespace TradingBot.Models;

public sealed class DailyPriceInfo
{
    public required DateOnly Date { get; init; }
    public required double ClosingPrice { get; init; }
}
