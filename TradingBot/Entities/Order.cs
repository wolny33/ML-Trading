namespace TradingBot.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public OrderDetails OrderDetails { get; set; }
        // details on why strategy decided to make this move
    }
}
