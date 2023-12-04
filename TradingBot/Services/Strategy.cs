using Microsoft.EntityFrameworkCore;
using Alpaca.Markets;
using TradingBot.Database;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IStrategy
{
    public Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync();
}

public sealed class Strategy : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IPricePredictor _predictor;

    public Strategy(IPricePredictor predictor, IDbContextFactory<AppDbContext> dbContextFactory,
        IAssetsDataSource assetsDataSource)
    {
        _predictor = predictor;
        _dbContextFactory = dbContextFactory;
        _assetsDataSource = assetsDataSource;
    }

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync()
    {
        var actions = await DetermineTradingActionsAsync(10);

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var action in actions) context.TradingActions.Add(action.ToEntity());

        await context.SaveChangesAsync();

        return actions;
    }

    private async Task<IReadOnlyList<TradingAction>> DetermineTradingActionsAsync(int maxBuyCount)
    {
        var predictions = await _predictor.GetPredictionsAsync();
        var assets = await _assetsDataSource.GetAssetsAsync();
        var tradingActions = new List<TradingAction>();

        var sellActions = new List<TradingAction>();
        var growthRates = new List<(TradingSymbol Symbol, decimal averageGrowthRate, decimal price)>();

        var cashAvaliable = assets.Cash.AvailableAmount;

        foreach (var prediction in predictions)
        {
            var symbol = prediction.Key;
            var closingPrices = prediction.Value.Prices.Select(dailyPrice => dailyPrice.ClosingPrice).ToList();

            if (IsPriceDecreasing(closingPrices) && assets.Positions.TryGetValue(symbol, out var position))
            {
                sellActions.Add(new TradingAction
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    Price = null,
                    Quantity = position.Quantity,
                    Symbol = symbol,
                    InForce = TimeInForce.Day,
                    OrderType = Models.OrderType.MarketSell
                });
            }
            else if (IsPriceIncreasing(closingPrices))
            {
                decimal growthRate = CalculateAverageGrowthRate(closingPrices);
                growthRates.Add((symbol, growthRate, closingPrices[0] + (closingPrices[1] - closingPrices[0]) / 2));
            }
        }

        tradingActions.AddRange(sellActions);
        tradingActions.AddRange(GetBuyActions(growthRates, cashAvaliable, maxBuyCount));

        return tradingActions;
    }

    private bool IsPriceDecreasing(List<decimal> closingPrices)
    {
        var isPriceDecreasing = true;

        for (int i = 1; i <= 5; i++)
        {
            if (closingPrices[i] > closingPrices[0])
            {
                isPriceDecreasing = false;
                break;
            }
        }

        return isPriceDecreasing;
    }

    private bool IsPriceIncreasing(List<decimal> closingPrices)
    {
        var isPriceIncreasing = true;

        for (int i = 1; i <= 10; i++)
        {
            if (closingPrices[i] < closingPrices[i - 1])
            {
                isPriceIncreasing = false;
                break;
            }
        }

        return isPriceIncreasing;
    }

    private decimal CalculateAverageGrowthRate(List<decimal> closingPrices)
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

    private List<TradingAction> GetBuyActions(List<(TradingSymbol Symbol, decimal averageGrowthRate, decimal price)> growthRates, decimal cashAvaliable, int maxBuyCount)
    {
        var buyActions = new List<TradingAction>();

        var topGrowingSymbols = growthRates
            .OrderByDescending(x => x.averageGrowthRate)
            .Take(maxBuyCount)
            .Select(x => (x.Symbol, x.price))
            .ToList();

        for (int i = 0; i < topGrowingSymbols.Count; i++)
        {
            var symbol = topGrowingSymbols[i].Symbol;
            var price = topGrowingSymbols[i].price;
            var quantity = (int)(cashAvaliable * 0.4m / price);

            if (i == topGrowingSymbols.Count - 1)
            {
                quantity = (int)Math.Ceiling(cashAvaliable / price);
            }

            if (quantity * price > cashAvaliable || quantity == 0)
                continue;

            cashAvaliable -= quantity * price;

            buyActions.Add(new TradingAction
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
                Price = price,
                Quantity = quantity,
                Symbol = symbol,
                InForce = TimeInForce.Day,
                OrderType = Models.OrderType.LimitBuy
            });
        }
        return buyActions;
    }
}
