using TradingBot.Database.Entities;

namespace TradingBot.Models;

public sealed class TradingActionDetails
{
    public required Guid Id { get; set; }

    // TODO: Details on why strategy decided to make this move

    public static TradingActionDetails FromEntity(TradingActionDetailsEntity entity)
    {
        return new TradingActionDetails
        {
            Id = entity.TradingActionId
        };
    }
}
