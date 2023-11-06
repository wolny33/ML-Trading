namespace TradingBot.Entities;

public sealed class Order
{
    public required Guid Id { get; set; }

    public required OrderDetails OrderDetails { get; set; }
    // details on why strategy decided to make this move
}
