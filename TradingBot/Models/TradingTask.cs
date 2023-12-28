using TradingBot.Database.Entities;
using TradingBot.Dto;

namespace TradingBot.Models;

/// <summary>
///     Represents single execution of decision flow
/// </summary>
public sealed class TradingTask
{
    public required Guid Id { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset? FinishedAt { get; init; }
    public required TradingTaskState State { get; init; }
    public required string StateDetails { get; init; }
    public required Guid? BacktestId { get; init; }

    public static TradingTask FromEntity(TradingTaskEntity entity)
    {
        return new TradingTask
        {
            Id = entity.Id,
            StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(entity.StartTimestamp),
            FinishedAt =
                entity.EndTimestamp is not null
                    ? DateTimeOffset.FromUnixTimeMilliseconds(entity.EndTimestamp.Value)
                    : null,
            State = entity.State,
            StateDetails = entity.StateDetails,
            BacktestId = entity.BacktestId
        };
    }

    public TradingTaskResponse ToResponse()
    {
        return new TradingTaskResponse
        {
            Id = Id,
            StartedAt = StartedAt,
            FinishedAt = FinishedAt,
            State = State.ToString(),
            StateDetails = StateDetails,
            BacktestId = BacktestId
        };
    }
}

public enum TradingTaskState
{
    ConfigDisabled,
    ExchangeClosed,
    Running,
    Success,
    Error
}
