using Microsoft.AspNetCore.Authentication;
using TradingBot.Configuration;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IStrategy
{
    public Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default);
}

public sealed class Strategy : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IPricePredictor _predictor;
    private readonly IMarketDataSource _marketDataSource;
    private readonly ISystemClock _clock;
    private readonly IStrategyParametersService _strategyParameters;

    public Strategy(IPricePredictor predictor, IAssetsDataSource assetsDataSource, IMarketDataSource marketDataSource,
        ISystemClock clock, IStrategyParametersService strategyParameters)
    {
        _predictor = predictor;
        _assetsDataSource = assetsDataSource;
        _marketDataSource = marketDataSource;
        _clock = clock;
        _strategyParameters = strategyParameters;
    }

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        var strategyParameters = await _strategyParameters.GetConfigurationAsync(token);
        var actions = await DetermineTradingActionsAsync(strategyParameters, token);

        return actions;
    }

    private async Task<IReadOnlyList<TradingAction>> DetermineTradingActionsAsync(
        StrategyParametersConfiguration strategyParameters, CancellationToken token = default)
    {
        var predictions = await _predictor.GetPredictionsAsync(token);
        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);
        var tradingActions = new List<TradingAction>();

        var sellActions = new List<TradingAction>();
        var growthRates = new List<AverageGrowthRate>();

        var cashAvailable = assets.Cash.BuyingPower;

        foreach (var prediction in predictions)
        {
            var symbol = prediction.Key;
            var closingPrices = new List<decimal>();
            var currentPrice = await GetCurrentPrice(symbol, token);
            closingPrices.Add(currentPrice);

            closingPrices.AddRange(prediction.Value.Prices.Select(dailyPrice => dailyPrice.ClosingPrice).ToList());

            if (IsPriceDecreasing(closingPrices, strategyParameters.MinDaysDecreasing) && assets.Positions.TryGetValue(symbol, out var position) && position.AvailableQuantity > 0)
            {
                sellActions.Add(TradingAction.MarketSell(symbol, position.AvailableQuantity, _clock.UtcNow));
            }
            else if (IsPriceIncreasing(closingPrices, strategyParameters.MinDaysIncreasing))
            {
                growthRates.Add(new AverageGrowthRate(symbol, CalculateAverageGrowthRate(closingPrices, strategyParameters.MinDaysIncreasing),
                    CalculateBuyLimitPrice(prediction, currentPrice)));
            }
        }

        tradingActions.AddRange(sellActions);
        tradingActions.AddRange(GetBuyActions(growthRates, cashAvailable, strategyParameters.MaxStocksBuyCount,
            strategyParameters.TopGrowingSymbolsBuyRatio));

        return tradingActions;
    }

    private async Task<decimal> GetCurrentPrice(TradingSymbol symbol, CancellationToken token = default)
    {
        var currentPrice = await _marketDataSource.GetLastAvailablePriceForSymbolAsync(symbol, token);

        return currentPrice;
    }

    private static bool IsPriceDecreasing(IReadOnlyList<decimal> closingPrices, int maxDaysDecreasing)
    {
        maxDaysDecreasing = maxDaysDecreasing > closingPrices.Count - 1 ? closingPrices.Count - 1 : maxDaysDecreasing;

        return closingPrices.Take(maxDaysDecreasing).Skip(1).All(price => price <= closingPrices[0]);
    }

    private static bool IsPriceIncreasing(IReadOnlyList<decimal> closingPrices, int maxDaysDecreasing)
    {
        var isPriceIncreasing = true;
        maxDaysDecreasing = maxDaysDecreasing >= closingPrices.Count ? closingPrices.Count - 1 : maxDaysDecreasing;

        for (int i = 1; i <= maxDaysDecreasing; i++)
        {
            if (closingPrices[i] < closingPrices[i - 1])
            {
                isPriceIncreasing = false;
                break;
            }
        }

        return isPriceIncreasing;
    }

    private static decimal CalculateAverageGrowthRate(IReadOnlyList<decimal> closingPrices, int minDaysIncreasing)
    {
        minDaysIncreasing = minDaysIncreasing >= closingPrices.Count ? closingPrices.Count - 1 : minDaysIncreasing;

        return (closingPrices[minDaysIncreasing - 1] - closingPrices[0]) / closingPrices[0];
    }

    private static decimal CalculateBuyLimitPrice(KeyValuePair<TradingSymbol, Prediction> prediction,
        decimal currentPrice)
    {
        var nextDayLowPrice = prediction.Value.Prices.Select(dailyPrice => dailyPrice.LowPrice).ToList()[0];
        var buyLimitPrice = (currentPrice + nextDayLowPrice) / 2;

        return buyLimitPrice;
    }

    private List<TradingAction> GetBuyActions(IReadOnlyList<AverageGrowthRate> growthRates, decimal cashAvailable,
        int maxBuyCount, double topGrowingSymbolsBuyRatio)
    {
        var buyActions = new List<TradingAction>();

        var topGrowingSymbols = growthRates
            .OrderByDescending(x => x.AvgGrowthRate)
            .Take(maxBuyCount)
            .Select(x => (x.Symbol, x.Price))
            .ToList();

        for (int i = 0; i < topGrowingSymbols.Count; i++)
        {
            var symbol = topGrowingSymbols[i].Symbol;
            var price = topGrowingSymbols[i].Price;
            if (price >= 1)
                price = Math.Round(price, 2);
            else
                price = Math.Round(price, 4);

            var quantity = (int)(cashAvailable * (decimal)topGrowingSymbolsBuyRatio / price);

            if (i == topGrowingSymbols.Count - 1)
                quantity = (int)(cashAvailable / price);

            if (quantity <= 0)
                continue;

            cashAvailable -= quantity * price;

            buyActions.Add(TradingAction.LimitBuy(symbol, quantity, price, _clock.UtcNow));
        }

        return buyActions;
    }

    private record AverageGrowthRate(TradingSymbol Symbol, decimal AvgGrowthRate, decimal Price);
}
