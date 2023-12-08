using TradingBot.Dto;

namespace TradingBot.Models;

public sealed class Assets
{
    public required decimal EquityValue { get; init; }
    public required Cash Cash { get; init; }
    public required IReadOnlyDictionary<TradingSymbol, Position> Positions { get; init; }

    public AssetsResponse ToResponse()
    {
        return new AssetsResponse
        {
            EquityValue = EquityValue,
            Cash = Cash.ToResponse(),
            Positions = Positions.Values.OrderByDescending(p => p.MarketValue).Select(p => p.ToResponse()).ToList()
        };
    }
}

public sealed class Position
{
    public required Guid SymbolId { get; init; }
    public required TradingSymbol Symbol { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal AvailableQuantity { get; init; }
    public required decimal MarketValue { get; init; }
    public required decimal AverageEntryPrice { get; init; }

    public PositionResponse ToResponse()
    {
        return new PositionResponse
        {
            Symbol = Symbol.Value,
            Quantity = Quantity,
            AvailableQuantity = AvailableQuantity,
            MarketValue = MarketValue,
            AverageEntryPrice = AverageEntryPrice
        };
    }
}

public sealed class Cash
{
    public required string MainCurrency { get; init; }
    public required decimal AvailableAmount { get; init; }
    public required decimal BuyingPower { get; init; }

    public CashResponse ToResponse()
    {
        return new CashResponse
        {
            MainCurrency = MainCurrency,
            AvailableAmount = AvailableAmount,
            BuyingPower = BuyingPower
        };
    }
}
