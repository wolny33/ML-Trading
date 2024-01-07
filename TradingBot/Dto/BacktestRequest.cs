using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class BacktestRequest : IValidatableObject
{
    /// <summary>
    ///     Earliest time at which a backtest was started (inclusive)
    /// </summary>
    public DateTimeOffset? Start { get; init; }

    /// <summary>
    ///     Latest time at which a backtest was started (inclusive)
    /// </summary>
    public DateTimeOffset? End { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this is { Start: { } start, End: { } end } && end <= start)
            yield return new ValidationResult($"'{nameof(Start)}' must be earlier than '{nameof(End)}'",
                new[] { nameof(Start), nameof(End) });
    }
}
