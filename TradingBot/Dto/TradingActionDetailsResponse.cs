using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public class TradingActionDetailsResponse
{
    [Required]
    public required Guid Id { get; init; }
}
