using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Alpaca.Markets;
using TradingBot.Exceptions.Alpaca;
using TradingBot.Models;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBot.Services;

public interface IBacktestAssets
{
    void InitializeForId(Guid backtestId, decimal initialCash);
    Assets GetForBacktestWithId(Guid backtestId);
    void PostActionForBacktest(TradingAction action, Guid backtestId);
    Task ExecuteQueuedActionsForBacktestAsync(Guid backtestId, DateOnly day);
}

public sealed class BacktestAssets : IBacktestAssets
{
    private readonly ConcurrentDictionary<Guid, Assets> _assets = new();
    private readonly ConcurrentDictionary<Guid, List<TradingAction>> _queuedActions = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public BacktestAssets(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

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

    public void PostActionForBacktest(TradingAction action, Guid backtestId)
    {
        if (!_assets.TryGetValue(backtestId, out var assets))
            throw new InvalidOperationException($"Assets were not initialized for backtest {backtestId}");

        ValidateAction(action, assets);

        switch (action.OrderType)
        {
            case OrderType.LimitSell or OrderType.MarketSell:
                ReserveAsset(action.Symbol, action.Quantity, backtestId);
                break;
            case OrderType.LimitBuy:
                ReserveCash(action.Quantity * action.Price!.Value, backtestId);
                break;
        }

        if (!_queuedActions.ContainsKey(backtestId)) _queuedActions[backtestId] = new List<TradingAction>();

        _queuedActions[backtestId].Add(action);
    }

    public async Task ExecuteQueuedActionsForBacktestAsync(Guid backtestId, DateOnly day)
    {
        if (!_queuedActions.TryGetValue(backtestId, out var actions) || !actions.Any()) return;

        var marketActions = actions.Where(a => a.OrderType is OrderType.MarketBuy or OrderType.MarketSell);
        var limitActions = actions.Where(a => a.OrderType is OrderType.LimitBuy or OrderType.LimitSell)
            .OrderBy(_ => Random.Shared.NextDouble());

        var sortedActions = marketActions.Concat(limitActions).ToList();

        using var scope = _scopeFactory.CreateScope();
        var marketDataSource = scope.ServiceProvider.GetRequiredService<IMarketDataSource>();

        foreach (var action in sortedActions)
        {
            var symbolData = (await marketDataSource.GetDataForSingleSymbolAsync(action.Symbol, day, day))
                ?.AsEnumerable().FirstOrDefault();
            if (symbolData is null)
                // TODO: cancel action in db
                continue;

            var shouldExecute = action.OrderType switch
            {
                // TODO: Include spread change
                OrderType.MarketBuy or OrderType.MarketSell => true,
                OrderType.LimitBuy => symbolData.Low <= action.Price,
                OrderType.LimitSell => symbolData.High >= action.Price,
                _ => throw new UnreachableException()
            };
            if (!shouldExecute)
                // TODO: cancel action in db
                continue;

            var isFullyFilled = action.OrderType switch
            {
                OrderType.LimitSell => SellAsset(
                    new BacktestTradeDetails(action.Symbol, action.Quantity, action.Price!.Value), backtestId),
                OrderType.LimitBuy => LimitBuyAsset(
                    new BacktestTradeDetails(action.Symbol, action.Quantity, action.Price!.Value), backtestId),
                OrderType.MarketSell => SellAsset(
                    new BacktestTradeDetails(action.Symbol, action.Quantity, symbolData.Open), backtestId),
                OrderType.MarketBuy => MarketBuyAsset(
                    new BacktestTradeDetails(action.Symbol, action.Quantity, symbolData.Open), backtestId),
                _ => throw new UnreachableException()
            };

            // TODO: Update action's state in db
        }
    }

    private static void ValidateAction(TradingAction action, Assets assets)
    {
        if (action.Quantity <= 0) throw new BadAlpacaRequestException("Quantity", "Quantity must be positive");

        if ((int)action.Quantity != action.Quantity &&
            (action.OrderType is not (OrderType.MarketBuy or OrderType.MarketSell) ||
             action.InForce != TimeInForce.Day))
            throw new InvalidFractionalOrderException();

        switch (action.OrderType)
        {
            case OrderType.LimitBuy when assets.Cash.AvailableAmount < action.Quantity * action.Price:
                throw new InsufficientFundsException();
            case OrderType.LimitSell or OrderType.MarketSell when
                !assets.Positions.TryGetValue(action.Symbol, out var position) ||
                position.AvailableQuantity < action.Quantity:
                throw new InsufficientAssetsException();
        }
    }

    private bool LimitBuyAsset(BacktestTradeDetails details, Guid backtestId)
    {
        var assets = _assets[backtestId];
        var newPositions = UpdateAsset(assets.Positions, details.Symbol, details.Amount, details.Price);
        var newAssets = new Assets
        {
            EquityValue = newPositions.Values.Aggregate(0m, (sum, position) => sum + position.MarketValue) +
                assets.Cash.AvailableAmount - details.Amount * details.Price,
            Cash = new Cash
            {
                MainCurrency = assets.Cash.MainCurrency,
                AvailableAmount = assets.Cash.AvailableAmount,
                BuyingPower = assets.Cash.BuyingPower - details.Amount * details.Price
            },
            Positions = newPositions
        };
        _assets[backtestId] = newAssets;

        return true;
    }

    private bool MarketBuyAsset(BacktestTradeDetails details, Guid backtestId)
    {
        var assets = _assets[backtestId];
        var correctedAmount = assets.Cash.BuyingPower / details.Price < details.Amount
            ? assets.Cash.BuyingPower / details.Price
            : details.Amount;
        var newPositions = UpdateAsset(assets.Positions, details.Symbol, correctedAmount, details.Price);
        var newAssets = new Assets
        {
            EquityValue = newPositions.Values.Aggregate(0m, (sum, position) => sum + position.MarketValue) +
                assets.Cash.AvailableAmount - correctedAmount * details.Price,
            Cash = new Cash
            {
                MainCurrency = assets.Cash.MainCurrency,
                AvailableAmount = assets.Cash.AvailableAmount,
                BuyingPower = assets.Cash.BuyingPower - correctedAmount * details.Price
            },
            Positions = newPositions
        };
        _assets[backtestId] = newAssets;

        return correctedAmount == details.Amount;
    }

    private bool SellAsset(BacktestTradeDetails details, Guid backtestId)
    {
        var assets = _assets[backtestId];
        var newPositions = UpdateAsset(assets.Positions, details.Symbol, -details.Amount, details.Price);
        var newAssets = new Assets
        {
            EquityValue = newPositions.Values.Aggregate(0m, (sum, position) => sum + position.MarketValue) +
                          assets.Cash.AvailableAmount + details.Amount * details.Price,
            Cash = new Cash
            {
                MainCurrency = assets.Cash.MainCurrency,
                AvailableAmount = assets.Cash.AvailableAmount + details.Amount * details.Price,
                BuyingPower = assets.Cash.BuyingPower + details.Amount * details.Price
            },
            Positions = newPositions
        };
        _assets[backtestId] = newAssets;

        return true;
    }

    private void ReserveAsset(TradingSymbol symbol, decimal amount, Guid backtestId)
    {
        var assets = _assets[backtestId];
        if (!assets.Positions.TryGetValue(symbol, out var position) || position.AvailableQuantity < amount)
            throw new InvalidOperationException("Not enough asset amount to execute requested action");

        var newPositions = assets.Positions.Values.ToDictionary(p => p.Symbol);
        newPositions[symbol] = new Position
        {
            Symbol = symbol,
            SymbolId = position.SymbolId,
            Quantity = position.Quantity,
            AvailableQuantity = position.AvailableQuantity - amount,
            MarketValue = position.MarketValue,
            AverageEntryPrice = position.AverageEntryPrice
        };

        var newAssets = new Assets
        {
            EquityValue = assets.EquityValue,
            Cash = new Cash
            {
                MainCurrency = assets.Cash.MainCurrency,
                AvailableAmount = assets.Cash.AvailableAmount,
                BuyingPower = assets.Cash.BuyingPower
            },
            Positions = newPositions
        };
        _assets[backtestId] = newAssets;
    }

    private void ReserveCash(decimal amount, Guid backtestId)
    {
        var assets = _assets[backtestId];
        if (assets.Cash.AvailableAmount < amount)
            throw new InvalidOperationException("Not enough money to execute requested action");

        var newAssets = new Assets
        {
            EquityValue = assets.EquityValue,
            Cash = new Cash
            {
                // TODO: check
                MainCurrency = assets.Cash.MainCurrency,
                AvailableAmount = assets.Cash.AvailableAmount - amount,
                BuyingPower = assets.Cash.BuyingPower
            },
            Positions = assets.Positions
        };
        _assets[backtestId] = newAssets;
    }

    private static IReadOnlyDictionary<TradingSymbol, Position> UpdateAsset(
        IReadOnlyDictionary<TradingSymbol, Position> positions, TradingSymbol symbol, decimal change, decimal price)
    {
        if (positions.TryGetValue(symbol, out var position))
        {
            var result = positions.Values.ToDictionary(p => p.Symbol);
            if (position.Quantity + change == 0)
            {
                result.Remove(symbol);
                return result;
            }

            result[symbol] = new Position
            {
                Symbol = symbol,
                SymbolId = position.SymbolId,
                Quantity = position.Quantity + change,
                AvailableQuantity = change > 0 ? position.AvailableQuantity + change : position.AvailableQuantity,
                MarketValue = (position.Quantity + change) * price,
                AverageEntryPrice = change > 0
                    ? (position.AverageEntryPrice * position.Quantity + change * price) / (position.Quantity + change)
                    : position.AverageEntryPrice
            };
            return result;
        }

        if (change < 0) throw new UnreachableException("Cannot sell not owned assets");

        return positions.Values.Append(new Position
        {
            Symbol = symbol,
            SymbolId = Guid.NewGuid(),
            Quantity = change,
            AvailableQuantity = change,
            MarketValue = change * price,
            AverageEntryPrice = price
        }).ToDictionary(p => p.Symbol);
    }

    private sealed record BacktestTradeDetails(TradingSymbol Symbol, decimal Amount, decimal Price);
}
