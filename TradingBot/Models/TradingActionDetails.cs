using TradingBot.Database.Entities;
using TradingBot.Dto;

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

    public TradingActionDetailsResponse ToResponse()
    {
        return new TradingActionDetailsResponse
        {
            Id = Id
        };
    }
}
