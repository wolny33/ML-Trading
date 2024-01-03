using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;

namespace TradingBot.Dto;

public sealed class BacktestCreationRequest : IValidatableObject
{
    [Required]
    public required DateOnly Start { get; init; }

    [Required]
    public required DateOnly End { get; init; }

    [Required]
    public required decimal InitialCash { get; init; }

    /// <summary>
    ///     If <c>true</c>, predictor will be used in the same way as in normal trading task execution. Otherwise,
    ///     predictor will instead return real future data, so that strategy algorithm can be tested.
    /// </summary>
    public bool ShouldUsePredictor { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (End <= Start)
            yield return new ValidationResult($"'{nameof(Start)}' must be earlier than '{nameof(End)}'",
                new[] { nameof(Start), nameof(End) });

        if (InitialCash <= 0)
            yield return new ValidationResult($"'{nameof(InitialCash)}' must be positive",
                new[] { nameof(InitialCash) });

        var clock = validationContext.GetRequiredService<ISystemClock>();
        if (ShouldUsePredictor && End >= DateOnly.FromDateTime(clock.UtcNow.LocalDateTime))
            yield return new ValidationResult($"'{nameof(End)}' must represent a past day (yesterday or earlier)",
                new[] { nameof(End) });

        if (!ShouldUsePredictor && End >= DateOnly.FromDateTime(clock.UtcNow.LocalDateTime).AddDays(-10))
            yield return new ValidationResult(
                $"'{nameof(End)}' must represent a day at least 10 days ago if predictor is not used",
                new[] { nameof(End), nameof(ShouldUsePredictor) });
    }
}
