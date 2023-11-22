using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class InvestmentResponse
{
    [Required]
    public required bool Enabled { get; init; }
}