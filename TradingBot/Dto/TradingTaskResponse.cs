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

    [Required]
    public required string StateDetails { get; init; }

    public required Guid? BacktestId { get; init; }
}
