using System.ComponentModel.DataAnnotations;
using TradingBot.Services;

namespace TradingBot.Dto;

public sealed class StrategySelectionRequest : IValidatableObject
{
    [Required]
    public string Name { get; init; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!StrategySelectionService.IsNameValid(Name))
        {
            yield return new ValidationResult($"'{Name}' is not a valid strategy name", new[] { nameof(Name) });
        }
    }
}
