using Flurl.Http;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IPricePredictor
{
    public Task<IDictionary<TradingSymbol, Prediction>> GetPredictionsAsync();
}

public sealed class PricePredictor : IPricePredictor
{
    private readonly ISystemClock _clock;
    private readonly IMarketDataSource _marketData;

    public PricePredictor(IMarketDataSource marketData, ISystemClock clock)
    {
        _marketData = marketData;
        _clock = clock;
    }

    public async Task<IDictionary<TradingSymbol, Prediction>> GetPredictionsAsync()
    {
        const int requiredDays = 10;
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var marketData = await _marketData.GetAllPricesAsync(today.AddDays(-requiredDays - 1), today);

        var result = new Dictionary<TradingSymbol, Prediction>();
        foreach (var (symbol, data) in marketData)
        {
            var prediction = await PredictForSymbolAsync(data);
            result[symbol] = prediction;
        }

        return result;
    }

    public static async Task<Prediction> PredictForSymbolAsync(IReadOnlyList<DailyTradingData> data)
    {
        var request = CreatePredictorRequest(data);

        using var client = new FlurlClient("http://predictor:8000");
        var response = await client.Request("predict").AllowAnyHttpStatus().PostJsonAsync(request);
        if (response.StatusCode != StatusCodes.Status200OK)
            throw new Exception($"Call failed with {response.StatusCode}: {await response.GetStringAsync()}");

        var predictorOutput = await response.GetJsonAsync<PredictorResponse>();

        return CreatePredictionFromPredictorOutput(predictorOutput, data[^1]);
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
                    Volume = RelativeChange(previous.Volume, current.Volume)
                };
            }).ToList()
        };
    }

    private static Prediction CreatePredictionFromPredictorOutput(PredictorResponse response,
        DailyTradingData lastDayData)
    {
        var result = new List<DailyPricePrediction>();
        foreach (var (closeChange, highChange, lowChange) in Enumerable.Range(0, response.Close.Count)
                     .Select(i => (response.Close[i], response.High[i], response.Low[i])))
            result.Add(new DailyPricePrediction
            {
                Date = (result.LastOrDefault()?.Date ?? lastDayData.Date).AddDays(1),
                ClosingPrice = (result.LastOrDefault()?.ClosingPrice ?? lastDayData.Close) * (1 + closeChange),
                HighPrice = (result.LastOrDefault()?.HighPrice ?? lastDayData.High) * (1 + highChange),
                LowPrice = (result.LastOrDefault()?.LowPrice ?? lastDayData.Low) * (1 + lowChange)
            });

        return new Prediction
        {
            Prices = result
        };
    }

    private static double RelativeChange(double previous, double now)
    {
        return (now - previous) / previous;
    }

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
        public required double Open { get; init; }

        [JsonProperty("close")]
        public required double Close { get; init; }

        [JsonProperty("high")]
        public required double High { get; init; }

        [JsonProperty("low")]
        public required double Low { get; init; }

        [JsonProperty("volume")]
        public required double Volume { get; init; }
    }

    private sealed class PredictorResponse
    {
        [JsonProperty("close")]
        public required List<double> Close { get; init; }

        [JsonProperty("high")]
        public required List<double> High { get; init; }

        [JsonProperty("low")]
        public required List<double> Low { get; init; }
    }
}
