using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class TradingTaskResponse
{
    [Required]
    public required Guid Id { get; init; }

    [Required]
    public required DateTimeOffset StartedAt { get; init; }

    public required DateTimeOffset? FinishedAt { get; init; }

    [Required]
    public required string State { get; init; }

    /// <summary>
    ///     Description of trading task's state
    /// </summary>
    /// <remarks>
    ///     Contains error details if an error occured during task's execution
    /// </remarks>
    [Required]
    public required string StateDetails { get; init; }

    public required Guid? BacktestId { get; init; }
}
