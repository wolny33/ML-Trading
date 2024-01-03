using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class ChangePasswordRequest
{
    [Required]
    public required string NewPassword { get; init; }
}
