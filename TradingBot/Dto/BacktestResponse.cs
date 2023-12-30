using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class BacktestResponse
{
    [Required]
    public required Guid Id { get; init; }

    [Required]
    public required DateOnly SimulationStart { get; init; }

    [Required]
    public required DateOnly SimulationEnd { get; init; }

    [Required]
    public required DateTimeOffset ExecutionStart { get; init; }

    public DateTimeOffset? ExecutionEnd { get; init; }

    [Required]
    public required string State { get; init; }

    [Required]
    public required string StateDetails { get; init; }

    [Required]
    public required double TotalReturn { get; init; }
}
