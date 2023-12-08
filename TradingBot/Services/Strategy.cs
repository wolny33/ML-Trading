using Microsoft.EntityFrameworkCore;
using Alpaca.Markets;
using TradingBot.Database;
using TradingBot.Models;
using Microsoft.AspNetCore.Authentication;
using SQLitePCL;
using TradingBot.Migrations;
using Newtonsoft.Json.Linq;

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
        var strategyParameters = await _strategyParameters.GetConfigurationAsync();
        var actions = await DetermineTradingActionsAsync(strategyParameters.MaxStocksBuyCount, strategyParameters.MinDaysDecreasing, strategyParameters.MinDaysIncreasing, strategyParameters.TopGrowingSymbolsBuyRatio, token);

        return actions;
    }

    private async Task<IReadOnlyList<TradingAction>> DetermineTradingActionsAsync(int maxBuyCount, int minDaysDecreasing, int minDaysIncreasing, decimal topGrowingSymbolsBuyRatio, CancellationToken token = default)
    {
        var predictions = await _predictor.GetPredictionsAsync();
        var assets = await _assetsDataSource.GetAssetsAsync();
        var tradingActions = new List<TradingAction>();

        var sellActions = new List<TradingAction>();
        var growthRates = new List<averageGrowthRate>();

        var cashAvailable = assets.Cash.BuyingPower;

        foreach (var prediction in predictions)
        {
            var symbol = prediction.Key;
            var closingPrices = new List<decimal>();
            var currentPrice = await GetCurrentPrice(symbol, token);
            if (currentPrice != null)
                closingPrices.Add(currentPrice.Close);

            closingPrices.AddRange(prediction.Value.Prices.Select(dailyPrice => dailyPrice.ClosingPrice).ToList());    

            if (IsPriceDecreasing(closingPrices, minDaysDecreasing) && assets.Positions.TryGetValue(symbol, out var position))
            {
                if (position.AvailableQuantity > 0)
                    sellActions.Add(TradingAction.MarketSell(symbol, position.AvailableQuantity, _clock.UtcNow));
            }
            else if (IsPriceIncreasing(closingPrices, minDaysIncreasing))
            {
                decimal growthRate = CalculateAverageGrowthRate(closingPrices);
                var lowPrice0 = prediction.Value.Prices.Select(dailyPrice => dailyPrice.LowPrice).ToList()[0];
                var lowPrice1 = prediction.Value.Prices.Select(dailyPrice => dailyPrice.LowPrice).ToList()[1];
                var buyLimitPrice = lowPrice0 + (lowPrice1 - lowPrice0) / 2;
                if(currentPrice != null)
                {
                    var tomorrowLowPrice = lowPrice0;
                    if (tomorrowLowPrice > currentPrice.Low)
                        buyLimitPrice = currentPrice.Low + (tomorrowLowPrice - currentPrice.Low) / 2;
                    else
                        buyLimitPrice = tomorrowLowPrice + (currentPrice.Low - tomorrowLowPrice) / 2;
                }

                growthRates.Add(new averageGrowthRate(symbol, growthRate, buyLimitPrice));
            }
        }

        tradingActions.AddRange(sellActions);
        tradingActions.AddRange(GetBuyActions(growthRates, cashAvailable, maxBuyCount, topGrowingSymbolsBuyRatio));

        return tradingActions;
    }

    private async Task<DailyTradingData?> GetCurrentPrice(TradingSymbol symbol, CancellationToken token = default)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var result = await _marketDataSource.GetDataForSingleSymbolAsync(symbol, today, today, token);

        return (result == null) ? null : result.First();
    }

    private static bool IsPriceDecreasing(IReadOnlyList<decimal> closingPrices, int maxDaysDecreasing)
    {
        var isPriceDecreasing = true;
        maxDaysDecreasing = maxDaysDecreasing > closingPrices.Count - 1 ? closingPrices.Count - 1 : maxDaysDecreasing;

        for (int i = 1; i <= maxDaysDecreasing; i++)
        {
            if (closingPrices[i] > closingPrices[0])
            {
                isPriceDecreasing = false;
                break;
            }
        }

        return isPriceDecreasing;
    }

    private static bool IsPriceIncreasing(IReadOnlyList<decimal> closingPrices, int maxDaysDecreasing)
    {
        var isPriceIncreasing = true;
        maxDaysDecreasing = maxDaysDecreasing > closingPrices.Count - 1 ? closingPrices.Count - 1 : maxDaysDecreasing;

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

    private static decimal CalculateAverageGrowthRate(IReadOnlyList<decimal> closingPrices)
    {
        decimal totalGrowth = 0;

        for (int i = 1; i < closingPrices.Count; i++)
        {
            decimal dailyGrowth = ((closingPrices[i] - closingPrices[i - 1]) / closingPrices[i - 1]) * 100;
            totalGrowth += dailyGrowth;
        }

        decimal averageGrowth = totalGrowth / (closingPrices.Count - 1);

        return averageGrowth;
    }

    private List<TradingAction> GetBuyActions(IReadOnlyList<averageGrowthRate> growthRates, decimal cashAvaliable, int maxBuyCount, decimal topGrowingSymbolsBuyRatio)
    {
        var buyActions = new List<TradingAction>();

        var topGrowingSymbols = growthRates
            .OrderByDescending(x => x.AverageGrowthRate)
            .Take(maxBuyCount)
            .Select(x => (x.Symbol, x.Price))
            .ToList();

        for (int i = 0; i < topGrowingSymbols.Count; i++)
        {
            var symbol = topGrowingSymbols[i].Symbol;
            var price = topGrowingSymbols[i].Price;
            if(price >= 1)
                price = Math.Round(price, 2);
            else
                price = Math.Round(price, 4);

            var quantity = (int)(cashAvaliable * topGrowingSymbolsBuyRatio / price);

            if (i == topGrowingSymbols.Count - 1)
            {
                quantity = (int)Math.Floor(cashAvaliable / price);
            }

            if (quantity * price > cashAvaliable || quantity <= 0)
                continue;

            cashAvaliable -= quantity * price;

            buyActions.Add(TradingAction.LimitBuy(symbol, quantity, price, _clock.UtcNow));
        }
        return buyActions;
    }

    private record averageGrowthRate(TradingSymbol Symbol, decimal AverageGrowthRate, decimal Price);
}
