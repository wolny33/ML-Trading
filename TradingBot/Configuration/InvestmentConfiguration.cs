using TradingBot.Database.Entities;
using TradingBot.Dto;

namespace TradingBot.Configuration;

public sealed class InvestmentConfiguration
{
    public required bool Enabled { get; init; }

    public static InvestmentConfiguration FromEntity(InvestmentConfigEntity entity)
    {
        return new InvestmentConfiguration
        {
            Enabled = entity.Enabled
        };
    }

    public InvestmentResponse ToResponse()
    {
        return new InvestmentResponse
        {
            Enabled = Enabled
        };
    }

    public static InvestmentConfigEntity CreateDefault()
    {
        return new InvestmentConfigEntity
        {
            Id = Guid.NewGuid(),
            Enabled = false
        };
    }
}
