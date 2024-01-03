using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public class TradingActionCollectionRequest : IValidatableObject
{
    public DateTimeOffset? Start { get; init; }
    public DateTimeOffset? End { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this is { Start: { } start, End: { } end } && end <= start)
            yield return new ValidationResult($"'{nameof(Start)}' must be earlier than '{nameof(End)}'",
                new[] { nameof(Start), nameof(End) });
    }
}
