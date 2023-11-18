using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class ChangePasswordRequest
{
    [Required]
    public string NewPassword { get; init; } = null!;
}
