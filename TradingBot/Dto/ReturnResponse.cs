using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class ReturnResponse
{
    /// <summary>
    ///     Relative change of portfolio value between first recorded state, and state at <see cref="Time" />
    /// </summary>
    [Required]
    public required decimal Return { get; init; }

    [Required]
    public required DateTimeOffset Time { get; init; }
}
