using TradingBot.Models;

namespace TradingBot.Services;

public sealed class GreedyStrategy : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IMarketDataSource _marketDataSource;
    private readonly IPricePredictor _predictor;
    private readonly ICurrentTradingTask _tradingTask;

    public GreedyStrategy(IAssetsDataSource assetsDataSource, IMarketDataSource marketDataSource,
        IPricePredictor predictor, ICurrentTradingTask tradingTask)
    {
        _assetsDataSource = assetsDataSource;
        _marketDataSource = marketDataSource;
        _predictor = predictor;
        _tradingTask = tradingTask;
    }

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        /*
         * Objective: most money (not stocks) after 5 days
         *
         * Analyze all scenarios that end with liquidating all assets by 5th day.
         * Don't analyze scenarios with 2 consecutive pass/hold days - assuming that high >= close and low <= close:
         *  - pass -> pass is equivalent to buy -> sell (of any stock)
         *  - hold -> hold is equivalent to sell -> buy (of currently held stock)
         * When buying stock, always choose the one that generates biggest return when selling after 2 or 3 days.
         *
         * If holds only money:
         *  Possible scenarios:
         *      pass -> 2best(1) -> 2best(3)
         *      pass -> 3best(1) -> pass
         *      2best(0) -> pass -> 2best(3)
         *      2best(0) -> 2best(2) -> pass
         *      2best(0) -> 3best(2)
         *      3best(0) -> 2best(3)
         *  Execute first action from best scenario
         *
         * If holds only 1 stock:
         *  Possible scenarios:
         *      sell -> pass -> 2best(2) -> pass
         *      sell -> pass -> 3best(2)
         *      sell -> 2best(1) -> 2best(3)
         *      sell -> 3best(1) -> pass
         *      pass -> sell -> pass -> 2best(3)
         *      pass -> sell -> 2best(2) -> pass
         *      pass -> sell -> 3best(2)
         *  Execute first action from best scenario
         *
         * If holds multiple stocks or single stock & money:
         *  Not caused by this strategy - sell all.
         *  Will do something smarter tomorrow.
         */
        
        var predictions = await _predictor.GetPredictionsAsync(token);
        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);

        // If holds multiple stocks...
        var heldPositionsCount = assets.Positions.Values.Count(position => position.Quantity > 0);
        var biggestHeldPosition = assets.Positions.Values.Where(position => position.Quantity > 0)
            .MaxBy(position => position.MarketValue);
        if (heldPositionsCount > 1) return GetSellAllAssetsActions(assets, predictions);

        // If haven't YOLO'd at least 90% into one stock...
        if (biggestHeldPosition is not null && biggestHeldPosition.MarketValue > assets.Cash.AvailableAmount * 9)
            return GetSellAllAssetsActions(assets, predictions);

        var normalizedPredictions = await GetNormalizedPredictionsAsync(predictions, token);
        var twoDayBest = DetermineBestTokensFor2DayBuy(normalizedPredictions);
        var threeDayBest = DetermineBestTokensFor3DayBuy(normalizedPredictions);

        // If holds single stock...
        if (biggestHeldPosition is not null)
        {
            var sellReturns = new[]
            {
                normalizedPredictions[biggestHeldPosition.Symbol].Prices[0].HighPrice,
                normalizedPredictions[biggestHeldPosition.Symbol].Prices[1].HighPrice
            };

            return GetActionsIfHoldsStock(new AnalysisDetails(twoDayBest, threeDayBest, predictions),
                biggestHeldPosition.Symbol,
                biggestHeldPosition.Quantity, sellReturns);
        }

        // If only holds money...
        return GetActionsIfHoldsMoney(new AnalysisDetails(twoDayBest, threeDayBest, predictions),
            assets.Cash.AvailableAmount);
    }

    private async Task<Dictionary<TradingSymbol, Prediction>> GetNormalizedPredictionsAsync(
        IDictionary<TradingSymbol, Prediction> predictions, CancellationToken token)
    {
        var lastPrices = await predictions.Keys.ToAsyncEnumerable()
            .SelectAwait(async symbol => (
                Symbol: symbol,
                LastPrice: await _marketDataSource.GetLastAvailablePriceForSymbolAsync(symbol, token)
            ))
            .ToDictionaryAsync(
                pair => pair.Symbol,
                pair => pair.LastPrice,
                token
            );

        var normalizedPredictions = new Dictionary<TradingSymbol, Prediction>();
        foreach (var (symbol, prediction) in predictions)
        {
            var lastPrice = lastPrices[symbol];
            var newPrediction = new Prediction
            {
                Prices = prediction.Prices.Select(prices => new DailyPricePrediction
                {
                    Date = prices.Date,
                    ClosingPrice = prices.ClosingPrice / lastPrice,
                    HighPrice = prices.HighPrice / lastPrice,
                    LowPrice = prices.LowPrice / lastPrice
                }).ToList()
            };

            normalizedPredictions[symbol] = newPrediction;
        }

        return normalizedPredictions;
    }

    private static IReadOnlyList<SymbolWithReturn> DetermineBestTokensFor2DayBuy(
        IDictionary<TradingSymbol, Prediction> normalizedPredictions)
    {
        return new List<SymbolWithReturn>
        {
            DetermineBestTokenForNDayBuyWithOffset(normalizedPredictions, 2, 0),
            DetermineBestTokenForNDayBuyWithOffset(normalizedPredictions, 2, 1),
            DetermineBestTokenForNDayBuyWithOffset(normalizedPredictions, 2, 2),
            DetermineBestTokenForNDayBuyWithOffset(normalizedPredictions, 2, 3)
        };
    }

    private static IReadOnlyList<SymbolWithReturn> DetermineBestTokensFor3DayBuy(
        IDictionary<TradingSymbol, Prediction> normalizedPredictions)
    {
        return new List<SymbolWithReturn>
        {
            DetermineBestTokenForNDayBuyWithOffset(normalizedPredictions, 3, 0),
            DetermineBestTokenForNDayBuyWithOffset(normalizedPredictions, 3, 1),
            DetermineBestTokenForNDayBuyWithOffset(normalizedPredictions, 3, 2)
        };
    }

    private static SymbolWithReturn DetermineBestTokenForNDayBuyWithOffset(
        IDictionary<TradingSymbol, Prediction> normalizedPredictions, int n, int offset)
    {
        if (normalizedPredictions.Values.Any(prediction => prediction.Prices.Count < n + offset - 1))
            throw new InvalidOperationException(
                $"Some predictions are shorter than {n + offset - 1} - can't determine best {n}-day buy on day {offset}");

        return normalizedPredictions.Keys.Select(symbol => new SymbolWithReturn(
                symbol,
                normalizedPredictions[symbol].Prices[n + offset - 1].HighPrice /
                normalizedPredictions[symbol].Prices[n].LowPrice)
            )
            .MaxBy(s => s.Return)!;
    }

    private IReadOnlyList<TradingAction> GetActionsIfHoldsMoney(AnalysisDetails details, decimal availableCash)
    {
        var (twoDayBest, threeDayBest, predictions) = details;

        var passReturn = new[]
        {
            twoDayBest[1].Return * twoDayBest[3].Return,
            threeDayBest[1].Return
        }.Max();

        var twoDayBestReturn = new[]
        {
            twoDayBest[0].Return * twoDayBest[3].Return,
            twoDayBest[0].Return * twoDayBest[2].Return,
            twoDayBest[0].Return * threeDayBest[2].Return
        }.Max();

        var threeDayBestReturn = threeDayBest[0].Return * twoDayBest[3].Return;

        if (twoDayBestReturn > passReturn && twoDayBestReturn >= threeDayBestReturn)
        {
            var toBuy = twoDayBest[0].Symbol;
            var price = predictions[toBuy].Prices[0].LowPrice;
            return new[]
            {
                TradingAction.LimitBuy(toBuy, availableCash / price, price, _tradingTask.GetTaskTime())
            };
        }

        if (threeDayBestReturn > passReturn && threeDayBestReturn > twoDayBestReturn)
        {
            var toBuy = threeDayBest[0].Symbol;
            var price = predictions[toBuy].Prices[0].LowPrice;
            return new[]
            {
                TradingAction.LimitBuy(toBuy, availableCash / price, price, _tradingTask.GetTaskTime())
            };
        }

        return Array.Empty<TradingAction>();
    }

    private IReadOnlyList<TradingAction> GetActionsIfHoldsStock(AnalysisDetails details, TradingSymbol held,
        decimal heldAmount, IReadOnlyList<decimal> sellReturns)
    {
        var (twoDayBest, threeDayBest, predictions) = details;

        var sellReturn = new[]
        {
            sellReturns[0] * twoDayBest[2].Return,
            sellReturns[0] * threeDayBest[2].Return,
            sellReturns[0] * twoDayBest[1].Return * twoDayBest[3].Return,
            sellReturns[0] * threeDayBest[1].Return
        }.Max();

        var holdReturn = new[]
        {
            sellReturns[1] * twoDayBest[3].Return,
            sellReturns[1] * twoDayBest[2].Return,
            sellReturns[1] * threeDayBest[2].Return
        }.Max();

        if (holdReturn >= sellReturn) return Array.Empty<TradingAction>();

        return new[]
        {
            TradingAction.LimitSell(held, heldAmount, predictions[held].Prices[0].HighPrice, _tradingTask.GetTaskTime())
        };
    }

    private IReadOnlyList<TradingAction> GetSellAllAssetsActions(Assets assets,
        IDictionary<TradingSymbol, Prediction> predictions)
    {
        var result = new List<TradingAction>();
        foreach (var (symbol, position) in assets.Positions)
        {
            if (position.Quantity <= 0) continue;

            result.Add(TradingAction.LimitSell(symbol, position.Quantity, predictions[symbol].Prices[0].HighPrice,
                _tradingTask.GetTaskTime()));
        }

        return result;
    }

    private sealed record SymbolWithReturn(TradingSymbol Symbol, decimal Return);

    private sealed record AnalysisDetails(IReadOnlyList<SymbolWithReturn> TwoDayBest,
        IReadOnlyList<SymbolWithReturn> ThreeDayBest, IDictionary<TradingSymbol, Prediction> Predictions);
}
