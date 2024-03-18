using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Alpaca.Markets;
using TradingBot.Exceptions.Alpaca;
using TradingBot.Models;
using ILogger = Serilog.ILogger;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBot.Services;

public interface IBacktestAssets
{
    void InitializeForId(Guid backtestId, decimal initialCash);
    Assets GetForBacktestWithId(Guid backtestId);
    Task PostActionForBacktestAsync(TradingAction action, Guid backtestId, DateOnly day);
    Task ExecuteQueuedActionsForBacktestAsync(Guid backtestId, DateOnly day, CancellationToken token = default);
}

public sealed class BacktestAssets : IBacktestAssets, IDisposable
{
    private readonly ITradingActionCommand _actionCommand;

    private readonly ConcurrentDictionary<Guid, Assets> _assets = new();
    private readonly ILogger _logger;
    private readonly Lazy<IMarketDataSource> _marketDataSource;
    private readonly ConcurrentDictionary<Guid, List<TradingAction>> _queuedActions = new();
    private readonly Lazy<IServiceScope> _scope;

    public BacktestAssets(IServiceScopeFactory scopeFactory, ITradingActionCommand actionCommand, ILogger logger)
    {
        _scope = new Lazy<IServiceScope>(scopeFactory.CreateScope);
        _marketDataSource =
            new Lazy<IMarketDataSource>(() => _scope.Value.ServiceProvider.GetRequiredService<IMarketDataSource>());
        _actionCommand = actionCommand;
        _logger = logger.ForContext<BacktestAssets>();
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

        _logger.Debug("Assets were initialized for backtest {Id} (initial cash: {Initial})", backtestId, initialCash);
    }

    public Assets GetForBacktestWithId(Guid backtestId)
    {
        if (!_assets.TryGetValue(backtestId, out var assets))
            throw new InvalidOperationException($"Assets were not initialized for backtest {backtestId}");

        return assets;
    }

    public async Task PostActionForBacktestAsync(TradingAction action, Guid backtestId, DateOnly day)
    {
        if (!_assets.TryGetValue(backtestId, out var assets))
            throw new InvalidOperationException($"Assets were not initialized for backtest {backtestId}");

        var symbolData = await GetTodayDataForSymbolAsync(action.Symbol, day.AddDays(1));
        ValidateAction(action, assets, symbolData);

        switch (action.OrderType)
        {
            case OrderType.LimitSell or OrderType.MarketSell:
                ReserveAsset(action.Symbol, action.Quantity, backtestId);
                break;
            case OrderType.LimitBuy:
                ReserveCash(action.Quantity * action.Price!.Value, backtestId);
                break;
            case OrderType.MarketBuy:
                if (symbolData is not null)
                    ReserveCash(action.Quantity * symbolData.Open, backtestId);
                break;
        }

        if (!_queuedActions.ContainsKey(backtestId)) _queuedActions[backtestId] = new List<TradingAction>();

        _queuedActions[backtestId].Add(action);

        _logger.Verbose("Trading action '{Details}' ({ActionId}) was posted for backtest {Id}",
            action.GetReadableString(), action.Id, backtestId);
    }

    public async Task ExecuteQueuedActionsForBacktestAsync(Guid backtestId, DateOnly day,
        CancellationToken token = default)
    {
        if (!_queuedActions.Remove(backtestId, out var actions) || !actions.Any())
        {
            _logger.Debug("No actions were posted for backtest {Id} for {Day}", backtestId, day);
            return;
        }

        foreach (var action in SortQueuedActions(actions).TakeWhile(_ => !token.IsCancellationRequested))
        {
            if (await GetTodayDataForSymbolAsync(action.Symbol, day) is not { } symbolData)
            {
                _logger.Warning("{Symbol} market data was missing for {Day}", action.Symbol, day);
                await ExpireActionAsync(action, backtestId);
                continue;
            }

            if (!WillActionExecute(action, symbolData))
            {
                _logger.Verbose("Action '{Details}' ({Id}) was not executed", action.GetReadableString(), action.Id);
                await ExpireActionAsync(action, backtestId);
                continue;
            }

            await ExecuteActionAndUpdateStateAsync(backtestId, action, symbolData);
        }

        await UpdateMarketValueOfHeldAssetsAsync(backtestId, day);
    }

    public void Dispose()
    {
        if (_scope.IsValueCreated) _scope.Value.Dispose();
    }

    private async Task UpdateMarketValueOfHeldAssetsAsync(Guid backtestId, DateOnly day)
    {
        var assets = _assets[backtestId];
        var prices = (await Task.WhenAll(
            assets.Positions.Values
                .Select(p => p.Symbol)
                .Select(async s => (Symbol: s, Price: (await GetTodayDataForSymbolAsync(s, day))?.Close))
        )).ToDictionary(pair => pair.Symbol, pair => pair.Price);

        var newPositions = assets.Positions.Values.Select(p => new Position
        {
            Symbol = p.Symbol,
            SymbolId = p.SymbolId,
            Quantity = p.Quantity,
            AvailableQuantity = p.AvailableQuantity,
            AverageEntryPrice = p.AverageEntryPrice,
            MarketValue = prices[p.Symbol] is { } price ? price * p.Quantity : p.MarketValue
        }).ToDictionary(p => p.Symbol);

        var newAssets = new Assets
        {
            EquityValue = newPositions.Values.Aggregate(0m, (sum, position) => sum + position.MarketValue) +
                          assets.Cash.AvailableAmount,
            Cash = assets.Cash,
            Positions = newPositions
        };
        _assets[backtestId] = newAssets;
    }

    private async Task ExpireActionAsync(TradingAction action, Guid backtestId)
    {
        switch (action.OrderType)
        {
            case OrderType.LimitSell or OrderType.MarketSell:
                FreeReservedAssets(action.Symbol, action.Quantity, backtestId);
                break;
            case OrderType.LimitBuy:
                FreeReservedCash(action.Quantity * action.Price!.Value, backtestId);
                break;
            case OrderType.MarketBuy:
                // Cash is not reserved for market buy orders that won't execute
                // (because they can only be skipped if there is no market data for requested symbol for that
                // day, so we don't know how much money to reserve)
                break;
            default:
                throw new UnreachableException();
        }

        await _actionCommand.UpdateBacktestActionStateAsync(action.Id,
            new BacktestActionState(OrderStatus.Expired, action.CreatedAt.AddDays(1)));
    }

    private async Task<DailyTradingData?> GetTodayDataForSymbolAsync(TradingSymbol symbol, DateOnly day)
    {
        var symbolData = await _marketDataSource.Value.GetDataForSingleSymbolAsync(symbol, day, day);
        return symbolData?.Count switch
        {
            null => null,
            1 => symbolData[0],
            _ => throw new UnreachableException()
        };
    }

    private async Task ExecuteActionAndUpdateStateAsync(Guid backtestId, TradingAction action,
        DailyTradingData symbolData)
    {
        switch (action.OrderType)
        {
            case OrderType.LimitSell:
                SellAsset(new BacktestTradeDetails(action.Symbol, action.Quantity, action.Price!.Value),
                    backtestId);
                break;
            case OrderType.LimitBuy:
                BuyAsset(new BacktestTradeDetails(action.Symbol, action.Quantity, action.Price!.Value),
                    backtestId);
                break;
            case OrderType.MarketSell:
                SellAsset(new BacktestTradeDetails(action.Symbol, action.Quantity, symbolData.Open), backtestId);
                break;
            case OrderType.MarketBuy:
                BuyAsset(new BacktestTradeDetails(action.Symbol, action.Quantity, symbolData.Open), backtestId);
                break;
            default:
                throw new UnreachableException();
        }

        await _actionCommand.UpdateBacktestActionStateAsync(action.Id,
            new BacktestActionState(
                OrderStatus.Filled,
                action.CreatedAt.AddHours(12),
                action.OrderType switch
                {
                    OrderType.LimitSell or OrderType.LimitBuy => action.Price!.Value,
                    OrderType.MarketSell or OrderType.MarketBuy => symbolData.Open,
                    _ => throw new UnreachableException()
                }
            ));
    }

    private static bool WillActionExecute(TradingAction action, DailyTradingData symbolData)
    {
        return action.OrderType switch
        {
            OrderType.MarketBuy or OrderType.MarketSell => true,
            OrderType.LimitBuy => symbolData.Low <= action.Price,
            OrderType.LimitSell => symbolData.High >= action.Price,
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    ///     Sorts queued actions to simulate execution order
    /// </summary>
    /// <remarks>
    ///     Market orders are placed first, in the order they were queued. Limit orders are shuffled randomly and placed
    ///     after market orders.
    /// </remarks>
    private static IReadOnlyList<TradingAction> SortQueuedActions(IReadOnlyList<TradingAction> actions)
    {
        var marketActions = actions.Where(a => a.OrderType is OrderType.MarketBuy or OrderType.MarketSell);
        var limitActions = actions.Where(a => a.OrderType is OrderType.LimitBuy or OrderType.LimitSell)
            .OrderBy(_ => Random.Shared.NextDouble());

        return marketActions.Concat(limitActions).ToList();
    }

    private void ValidateAction(TradingAction action, Assets assets, DailyTradingData? symbolData)
    {
        if (action.Quantity <= 0) throw new BadAlpacaRequestException("Quantity", "Quantity must be positive");

        if ((int)action.Quantity != action.Quantity &&
            (action.OrderType is not (OrderType.MarketBuy or OrderType.MarketSell) ||
             action.InForce != TimeInForce.Day))
            throw new InvalidFractionalOrderException();

        switch (action.OrderType)
        {
            case OrderType.LimitBuy when assets.Cash.BuyingPower < action.Quantity * action.Price:
                throw new InsufficientFundsException();
            case OrderType.LimitSell or OrderType.MarketSell when
                !assets.Positions.TryGetValue(action.Symbol, out var position) ||
                position.AvailableQuantity < action.Quantity:
                throw new InsufficientAssetsException();
            case OrderType.MarketBuy when symbolData is not null &&
                                          assets.Cash.BuyingPower < action.Quantity * symbolData.Open:
                throw new InsufficientFundsException();
        }

        _logger.Verbose("Trading action {Details} ({ActionId}) was successfully validated", action.GetReadableString(),
            action.Id);
    }

    private void BuyAsset(BacktestTradeDetails details, Guid backtestId)
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
                AvailableAmount = assets.Cash.AvailableAmount - details.Amount * details.Price,
                BuyingPower = assets.Cash.BuyingPower
            },
            Positions = newPositions
        };
        _assets[backtestId] = newAssets;

        _logger.Verbose("{Amount} {Symbol} was bought at {Price}", details.Amount, details.Symbol.Value,
            details.Price);
    }

    private void SellAsset(BacktestTradeDetails details, Guid backtestId)
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

        _logger.Verbose("{Amount} {Symbol} was sold at {Price}", details.Amount, details.Symbol.Value, details.Price);
    }

    private void ReserveAsset(TradingSymbol symbol, decimal amount, Guid backtestId)
    {
        var assets = _assets[backtestId];
        if (!assets.Positions.TryGetValue(symbol, out var position) || position.AvailableQuantity < amount)
            throw new UnreachableException("Not enough asset amount to reserve - action validation failed");

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
            Cash = assets.Cash,
            Positions = newPositions
        };
        _assets[backtestId] = newAssets;

        _logger.Verbose("{Amount} of {Symbol} was reserved in backtest {Id}", amount, symbol.Value, backtestId);
    }

    private void ReserveCash(decimal amount, Guid backtestId)
    {
        var assets = _assets[backtestId];
        if (assets.Cash.BuyingPower < amount)
            throw new UnreachableException("Not enough money to reserve - action validation failed");

        var newAssets = new Assets
        {
            EquityValue = assets.EquityValue,
            Cash = new Cash
            {
                MainCurrency = assets.Cash.MainCurrency,
                AvailableAmount = assets.Cash.AvailableAmount,
                BuyingPower = assets.Cash.BuyingPower - amount
            },
            Positions = assets.Positions
        };
        _assets[backtestId] = newAssets;

        _logger.Verbose("{Amount} USD was reserved in backtest {Id}", amount, backtestId);
    }

    private void FreeReservedAssets(TradingSymbol symbol, decimal amount, Guid backtestId)
    {
        var assets = _assets[backtestId];
        if (!assets.Positions.TryGetValue(symbol, out var position) ||
            position.AvailableQuantity + amount > position.Quantity)
            throw new UnreachableException("Assets cannot be freed");

        var newPositions = assets.Positions.Values.ToDictionary(p => p.Symbol);
        newPositions[symbol] = new Position
        {
            Symbol = symbol,
            SymbolId = position.SymbolId,
            Quantity = position.Quantity,
            AvailableQuantity = position.AvailableQuantity + amount,
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

        _logger.Verbose("{Amount} of {Symbol} was freed in backtest {Id}", amount, symbol.Value, backtestId);
    }

    private void FreeReservedCash(decimal amount, Guid backtestId)
    {
        var assets = _assets[backtestId];
        if (assets.Cash.BuyingPower + amount > assets.Cash.AvailableAmount)
            throw new UnreachableException("Money cannot be freed");

        var newAssets = new Assets
        {
            EquityValue = assets.EquityValue,
            Cash = new Cash
            {
                MainCurrency = assets.Cash.MainCurrency,
                AvailableAmount = assets.Cash.AvailableAmount,
                BuyingPower = assets.Cash.BuyingPower + amount
            },
            Positions = assets.Positions
        };
        _assets[backtestId] = newAssets;

        _logger.Verbose("{Amount} USD was freed in backtest {Id}", amount, backtestId);
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

        if (change < 0) throw new UnreachableException("Cannot sell not owned assets - action validation failed");

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

    /// <summary>
    ///     Used for unit tests
    /// </summary>
    internal void SetAssetsForBacktest(Guid backtestId, Assets assets)
    {
        _assets[backtestId] = assets;
    }

    private sealed record BacktestTradeDetails(TradingSymbol Symbol, decimal Amount, decimal Price);
}
