namespace TradingBot.Services.AlpacaClients;

public sealed class AssetResponse
{
    public required Guid Id { get; init; }
    public required string Symbol { get; init; }
    public required bool Tradable { get; init; }
    public required bool Fractionable { get; init; }
}
