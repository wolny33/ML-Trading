using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;

namespace TradingBot.Dto;

public sealed class BacktestCreationRequest : IValidatableObject
{
    [Required]
    public DateOnly Start { get; init; }

    [Required]
    public DateOnly End { get; init; }

    [Required]
    public decimal InitialCash { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (End <= Start)
            yield return new ValidationResult($"'{nameof(Start)}' must be earlier than '{nameof(End)}'",
                new[] { nameof(Start), nameof(End) });

        if (InitialCash <= 0)
            yield return new ValidationResult($"'{nameof(InitialCash)}' must be positive",
                new[] { nameof(InitialCash) });

        var clock = validationContext.GetRequiredService<ISystemClock>();
        if (End >= DateOnly.FromDateTime(clock.UtcNow.LocalDateTime))
            yield return new ValidationResult($"'{nameof(End)}' must represent a past day (yesterday or earlier)",
                new[] { nameof(End) });
    }
}
