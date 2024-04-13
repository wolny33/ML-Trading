using TradingBot.Database.Entities;
using TradingBot.Dto;

namespace TradingBot.Models;

public sealed class Backtest
{
    public required Guid Id { get; init; }
    public required DateOnly SimulationStart { get; init; }
    public required DateOnly SimulationEnd { get; init; }
    public required DateTimeOffset ExecutionStart { get; init; }
    public DateTimeOffset? ExecutionEnd { get; init; }
    public required bool UsePredictor { get; init; }
    public required double MeanPredictorError { get; init; }
    public required BacktestState State { get; init; }
    public required string StateDetails { get; init; }
    public required string Description { get; init; }
    public required double TotalReturn { get; init; }

    public static Backtest FromEntity(BacktestEntity entity)
    {
        return new Backtest
        {
            Id = entity.Id,
            SimulationStart = entity.SimulationStart,
            SimulationEnd = entity.SimulationEnd,
            ExecutionStart = DateTimeOffset.FromUnixTimeMilliseconds(entity.ExecutionStartTimestamp),
            ExecutionEnd = entity.ExecutionEndTimestamp is not null
                ? DateTimeOffset.FromUnixTimeMilliseconds(entity.ExecutionEndTimestamp.Value)
                : null,
            UsePredictor = entity.UsePredictor,
            MeanPredictorError = entity.MeanPredictorError,
            State = entity.State,
            StateDetails = entity.StateDetails,
            Description = entity.Description,
            TotalReturn = CalculateTotalReturn(entity.AssetsStates.AsReadOnly())
        };
    }

    private static double CalculateTotalReturn(IReadOnlyList<AssetsStateEntity> assets)
    {
        var first = assets.MinBy(a => a.CreationTimestamp);
        var last = assets.MaxBy(a => a.CreationTimestamp);

        if (first?.EquityValue is { } startValue && last?.EquityValue is { } endValue)
            return (endValue - startValue) / startValue;

        return 0;
    }

    public BacktestResponse ToResponse()
    {
        return new BacktestResponse
        {
            Id = Id,
            SimulationStart = SimulationStart,
            SimulationEnd = SimulationEnd,
            ExecutionStart = ExecutionStart,
            ExecutionEnd = ExecutionEnd,
            UsePredictor = UsePredictor,
            MeanPredictorError = MeanPredictorError,
            State = State.ToString(),
            StateDetails = StateDetails,
            Description = Description,
            TotalReturn = TotalReturn
        };
    }
}

public enum BacktestState
{
    Running,
    Finished,
    Error,
    Cancelled
}
