using TradingBot.Models;

namespace TradingBot.Services;

public interface IAssetsDataSource
{
    public Task<Assets> GetAssetsAsync(CancellationToken token = default);
}

public sealed class AssetsDataSource : IAssetsDataSource
{
    private readonly IAlpacaClientFactory _clientFactory;

    public AssetsDataSource(IAlpacaClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public Task<Assets> GetAssetsAsync(CancellationToken token = default)
    {
        return Task.FromResult(new Assets
        {
            EquityValue = 11015.98m + 12.04m * 70.66m + 128.97m * 13.06m,
            Cash = new Cash
            {
                AvailableAmount = 11015.98m,
                MainCurrency = "USD"
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("AMZN")] = new()
                {
                    SymbolId = Guid.NewGuid(),
                    Symbol = new TradingSymbol("AMZN"),
                    Quantity = 12.04m,
                    AvailableQuantity = 12.04m,
                    AverageEntryPrice = 67.54m,
                    MarketValue = 12.04m * 70.66m
                },
                [new TradingSymbol("BBBY")] = new()
                {
                    SymbolId = Guid.NewGuid(),
                    Symbol = new TradingSymbol("BBBY"),
                    Quantity = 128.97m,
                    AvailableQuantity = 58.97m,
                    AverageEntryPrice = 14.87m,
                    MarketValue = 128.97m * 13.06m
                }
            }
        });
    }
}
