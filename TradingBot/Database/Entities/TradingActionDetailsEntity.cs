using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class TradingActionDetailsEntity
{
    [Key]
    public required Guid TradingActionId { get; init; }

    [Required]
    public TradingActionEntity TradingAction { get; init; } = null!;
}
