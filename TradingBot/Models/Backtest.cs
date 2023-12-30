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
    public required BacktestState State { get; init; }
    public required string StateDetails { get; init; }

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
            State = entity.State,
            StateDetails = entity.StateDetails
        };
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
            State = State.ToString(),
            StateDetails = StateDetails
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
