using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingBot.Database.Entities;

public sealed class UserCredentialsEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; init; }

    public required string Username { get; init; }
    public required string HashedPassword { get; set; }
}
