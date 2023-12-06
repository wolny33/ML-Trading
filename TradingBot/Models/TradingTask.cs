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
}

public enum TradingTaskState
{
    ConfigDisabled,
    ExchangeClosed,
    Running,
    Success,
    Error
}
