using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class UserCredentialsEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required string Username { get; init; }
    public required string HashedPassword { get; set; }
}
