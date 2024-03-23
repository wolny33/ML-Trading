using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class WinnerSymbolToBuyEntity
{
    [Key]
    public required Guid Id { get; init; }

    [Required]
    public BuyWinnersEvaluationEntity Evaluation { get; init; } = null!;

    public required Guid EvaluationId { get; init; }
    public required string Symbol { get; init; }
}
