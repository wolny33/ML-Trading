using TradingBot.Database.Entities;
using TradingBot.Dto;

namespace TradingBot.Configuration;

public sealed class TestModeConfiguration
{
    public required bool Enabled { get; init; }

    public static TestModeConfiguration FromEntity(TestModeConfigEntity entity)
    {
        return new TestModeConfiguration
        {
            Enabled = entity.Enabled
        };
    }

    public TestModeResponse ToResponse()
    {
        return new TestModeResponse
        {
            Enabled = Enabled
        };
    }

    public static TestModeConfigEntity CreateDefault()
    {
        return new TestModeConfigEntity
        {
            Id = Guid.NewGuid(),
            Enabled = true
        };
    }
}
