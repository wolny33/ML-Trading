using Flurl.Http;
using Flurl.Http.Configuration;
using MathNet.Numerics.Distributions;
using Newtonsoft.Json;
using TradingBot.Exceptions;
using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IPricePredictor
{
    Task<IDictionary<TradingSymbol, Prediction>> GetPredictionsAsync(CancellationToken token = default);
    Task<Prediction?> GetPredictionForSingleSymbolAsync(TradingSymbol symbol, CancellationToken token = default);
}

public sealed class PricePredictor : IPricePredictor
{
    private const int PredictorInputLength = 10;
    public const int PredictorOutputLength = 5;

    private readonly IFlurlClientFactory _flurlFactory;
    private readonly ILogger _logger;
    private readonly IMarketDataSource _marketData;
    private readonly ICurrentTradingTask _tradingTask;

    public PricePredictor(IMarketDataSource marketData, IFlurlClientFactory flurlFactory, ILogger logger,
        ICurrentTradingTask tradingTask)
    {
        _marketData = marketData;
        _flurlFactory = flurlFactory;
        _tradingTask = tradingTask;
        _logger = logger.ForContext<PricePredictor>();
    }

    public async Task<IDictionary<TradingSymbol, Prediction>> GetPredictionsAsync(CancellationToken token = default)
    {
        var today = _tradingTask.GetTaskDay();

        if (_tradingTask.ShouldReturnFutureDataFromPredictor) return await GetFutureDataAsync(today, token);

        _logger.Debug("Getting predictions for {Today}", today);
        var marketData =
            await _marketData.GetPricesAsync(SubtractWorkDays(today, 2 * (PredictorInputLength + 1)), today, token);

        var result = new Dictionary<TradingSymbol, Prediction>();
        foreach (var (symbol, data) in marketData)
        {
            if (data.Count < PredictorInputLength + 1)
                continue;
            _logger.Verbose("Getting predictions for {Token}", symbol.Value);
            var prediction = await PredictForSymbolAsync(data, token);
            result[symbol] = prediction;
        }

        return result;
    }

    public async Task<Prediction?> GetPredictionForSingleSymbolAsync(TradingSymbol symbol,
        CancellationToken token = default)
    {
        var today = _tradingTask.GetTaskDay();

        if (_tradingTask.ShouldReturnFutureDataFromPredictor)
            return await GetFutureDataForSingleSymbolAsync(symbol, today, token);

        var marketData = await _marketData.GetDataForSingleSymbolAsync(symbol,
            SubtractWorkDays(today, 2 * (PredictorInputLength + 1)), today, token);
        if (marketData is null || marketData.Count < PredictorInputLength + 1) return null;

        _logger.Verbose("Getting predictions for {Today} for single token: {Token}", today, symbol.Value);
        return await PredictForSymbolAsync(marketData, token);
    }

    private async Task<Prediction> PredictForSymbolAsync(IReadOnlyList<DailyTradingData> data,
        CancellationToken token = default)
    {
        var request = CreatePredictorRequest(data);
        var response = await SendRequestAsync(request, token);
        return CreatePredictionFromPredictorOutput(response, data[^1]);
    }

    private async Task<IDictionary<TradingSymbol, Prediction>> GetFutureDataAsync(DateOnly today,
        CancellationToken token)
    {
        _logger.Debug("Returning future data instead of predictions for {Today}", today);

        var futureData =
            await _marketData.GetPricesAsync(today.AddDays(1), today.AddDays(2 * PredictorOutputLength), token);

        var pastData = await _marketData.GetPricesAsync(today.AddDays(-5), today, token);
        var latestData = pastData.ToDictionary(pair => pair.Key, pair => pair.Value[^1]);

        var futureDataResult = new Dictionary<TradingSymbol, Prediction>();
        foreach (var (symbol, data) in futureData)
        {
            futureDataResult[symbol] = new Prediction
            {
                Prices = data.Take(PredictorOutputLength)
                    .Select((d, i) => MakeFakePredictionFromDailyData(d, i > 0 ? data[i - 1] : latestData[symbol]))
                    .ToList()
            };
        }

        return futureDataResult;
    }

    private DailyPricePrediction MakeFakePredictionFromDailyData(DailyTradingData today, DailyTradingData previous)
    {
        var distribution =
            new Normal(0, _tradingTask.MeanPredictionError * double.Sqrt(double.Pi / 2.0), Random.Shared);
        var errors = distribution.Samples().Take(3).ToList();
        return new DailyPricePrediction
        {
            Date = today.Date,
            ClosingPrice = today.Close + (decimal)errors[0] * previous.Close,
            HighPrice = today.High + (decimal)errors[1] * previous.High,
            LowPrice = today.Low + (decimal)errors[2] * previous.Low
        };
    }

    private async Task<Prediction?> GetFutureDataForSingleSymbolAsync(TradingSymbol symbol,
        DateOnly today, CancellationToken token)
    {
        _logger.Debug("Returning future data instead of predictions for {Today}", today);
        var futureData = await _marketData.GetDataForSingleSymbolAsync(symbol, today.AddDays(1),
            today.AddDays(2 * PredictorOutputLength), token);
        var latestData = (await _marketData.GetDataForSingleSymbolAsync(symbol, today.AddDays(-5),
            today, token))?[^1];

        if (futureData is null || latestData is null) return null;

        return new Prediction
        {
            Prices = futureData.Take(PredictorOutputLength)
                .Select((d, i) => MakeFakePredictionFromDailyData(d, i > 0 ? futureData[i - 1] : latestData))
                .ToList()
        };
    }

    private static PredictorRequest CreatePredictorRequest(IReadOnlyList<DailyTradingData> data)
    {
        return new PredictorRequest
        {
            Data = data.Skip(1).Select((current, index) =>
            {
                var previous = data[index];
                return new SingleDayRequest
                {
                    Date = current.Date.ToString("O"),
                    Open = RelativeChange(previous.Open, current.Open),
                    Close = RelativeChange(previous.Close, current.Close),
                    High = RelativeChange(previous.High, current.High),
                    Low = RelativeChange(previous.Low, current.Low),
                    Volume = current.Volume,
                    FearGreedIndex = current.FearGreedIndex
                };
            }).Take(PredictorInputLength).ToList()
        };
    }

    private async Task<PredictorResponse> SendRequestAsync(PredictorRequest request,
        CancellationToken token = default)
    {
        _logger.Verbose("Sending request to predictor service");
        using var client = _flurlFactory.Get("http://predictor:8000");

        try
        {
            var response = await client.Request("predict").AllowAnyHttpStatus().PostJsonAsync(request, token);
            return response.StatusCode switch
            {
                StatusCodes.Status200OK => await response.GetJsonAsync<PredictorResponse>(),
                var code => throw new UnsuccessfulPredictorResponseException(code, await response.GetStringAsync())
            };
        }
        catch (FlurlHttpException e)
        {
            _logger.Error(e, "Predictor call failed");
            throw new PredictorCallFailedException(e);
        }
    }

    private static Prediction CreatePredictionFromPredictorOutput(PredictorResponse response,
        DailyTradingData lastDayData)
    {
        var result = new List<DailyPricePrediction>();
        foreach (var prediction in response.Predictions)
            result.Add(CreateDailyPrediction(lastDayData, result.LastOrDefault(), prediction));

        return new Prediction
        {
            Prices = result
        };
    }

    private static DailyPricePrediction CreateDailyPrediction(DailyTradingData lastDayData,
        DailyPricePrediction? previous, SingleDayPrediction prediction)
    {
        var lastDay = previous?.Date ?? lastDayData.Date;
        return new DailyPricePrediction
        {
            Date = lastDay.DayOfWeek switch
            {
                // Next weekday
                DayOfWeek.Friday => lastDay.AddDays(3),
                DayOfWeek.Saturday => lastDay.AddDays(2),
                _ => lastDay.AddDays(1)
            },
            ClosingPrice = (previous?.ClosingPrice ?? lastDayData.Close) * (1 + prediction.CloseChange),
            HighPrice = (previous?.HighPrice ?? lastDayData.High) * (1 + prediction.HighChange),
            LowPrice = (previous?.LowPrice ?? lastDayData.Low) * (1 + prediction.LowChange)
        };
    }

    private static decimal RelativeChange(decimal previous, decimal now)
    {
        return (now - previous) / previous;
    }

    private static DateOnly SubtractWorkDays(DateOnly date, int count)
    {
        for (var i = count; i > 0; i--)
            date = date.AddDays(date.DayOfWeek switch
            {
                DayOfWeek.Monday => -3,
                DayOfWeek.Sunday => -2,
                _ => -1
            });

        return date;
    }

    #region Predictor service DTO

    private sealed class PredictorRequest
    {
        [JsonProperty("data")]
        public required IReadOnlyList<SingleDayRequest> Data { get; init; }
    }

    private sealed class SingleDayRequest
    {
        [JsonProperty("date")]
        public required string Date { get; init; }

        [JsonProperty("open")]
        public required decimal Open { get; init; }

        [JsonProperty("close")]
        public required decimal Close { get; init; }

        [JsonProperty("high")]
        public required decimal High { get; init; }

        [JsonProperty("low")]
        public required decimal Low { get; init; }

        [JsonProperty("volume")]
        public required decimal Volume { get; init; }

        [JsonProperty("fearGreedIndex")]
        public required decimal FearGreedIndex { get; init; }
    }

    private sealed class PredictorResponse
    {
        [JsonProperty("predictions")]
        public required List<SingleDayPrediction> Predictions { get; init; }
    }

    private sealed class SingleDayPrediction
    {
        [JsonProperty("close")]
        public required decimal CloseChange { get; init; }

        [JsonProperty("high")]
        public required decimal HighChange { get; init; }

        [JsonProperty("low")]
        public required decimal LowChange { get; init; }
    }

    #endregion
}
