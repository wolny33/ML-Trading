using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public class InvestmentRequest
{
    [Required]
    public required bool Enable { get; init; }
}
