using System.ComponentModel.DataAnnotations;

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
    }
}
