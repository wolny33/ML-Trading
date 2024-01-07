using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class BacktestResponse
{
    [Required]
    public required Guid Id { get; init; }

    /// <summary>
    ///     First day at which a simulated trading task will be executed
    /// </summary>
    /// <remarks>
    ///     During backtests, trading tasks are executed after market closes on a given day. That means that orders
    ///     posted during the first day will be executed during the second day.
    /// </remarks>
    [Required]
    public required DateOnly SimulationStart { get; init; }

    /// <summary>
    ///     The day after the last day at which a simulated trading task will be executed
    /// </summary>
    /// <remarks>
    ///     This day is the last day at which orders will be processed. See remark in <see cref="SimulationStart" />.
    /// </remarks>
    [Required]
    public required DateOnly SimulationEnd { get; init; }

    [Required]
    public required DateTimeOffset ExecutionStart { get; init; }

    public DateTimeOffset? ExecutionEnd { get; init; }

    /// <summary>
    ///     If <c>true</c>, predictor was used during this backtest, just like during normal trading task execution.
    ///     Otherwise, predictor was not used, and real future data was returned in place of predictions. This means
    ///     that only the strategy algorithm was tested.
    /// </summary>
    [Required]
    public required bool UsePredictor { get; init; }

    [Required]
    public required string State { get; init; }

    [Required]
    public required string StateDetails { get; init; }

    [Required(AllowEmptyStrings = true)]
    public required string Description { get; init; }

    /// <summary>
    ///     Relative change of value of held assets between backtest start and last recorded state
    /// </summary>
    [Required]
    public required double TotalReturn { get; init; }
}
