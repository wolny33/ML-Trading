using System.Collections.Concurrent;
using System.Collections.Immutable;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IBacktestAssets
{
    void InitializeForId(Guid backtestId, decimal initialCash);
    Assets GetForBacktestWithId(Guid backtestId);
}

public sealed class BacktestAssets : IBacktestAssets
{
    private readonly ConcurrentDictionary<Guid, Assets> _assets = new();

    public void InitializeForId(Guid backtestId, decimal initialCash)
    {
        if (_assets.ContainsKey(backtestId))
            throw new InvalidOperationException($"Assets were already initialized for backtest {backtestId}");

        _assets[backtestId] = new Assets
        {
            EquityValue = initialCash,
            Cash = new Cash
            {
                AvailableAmount = initialCash,
                BuyingPower = initialCash,
                MainCurrency = "USD"
            },
            Positions = ImmutableDictionary<TradingSymbol, Position>.Empty
        };
    }

    public Assets GetForBacktestWithId(Guid backtestId)
    {
        if (!_assets.TryGetValue(backtestId, out var assets))
            throw new InvalidOperationException($"Assets were not initialized for backtest {backtestId}");

        return assets;
    }
}
