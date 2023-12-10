﻿using TradingBot.Database.Entities;

namespace TradingBot.Models;

public sealed record AssetsState(Assets Assets, DateTimeOffset CreatedAt)
{
    public AssetsStateEntity ToEntity()
    {
        var id = Guid.NewGuid();
        return new AssetsStateEntity
        {
            Id = id,
            CreationTimestamp = CreatedAt.ToUnixTimeMilliseconds(),
            MainCurrency = Assets.Cash.MainCurrency,
            EquityValue = (double)Assets.EquityValue,
            AvailableCash = (double)Assets.Cash.AvailableAmount,
            HeldPositions = Assets.Positions.Values.Select(p => new PositionEntity
            {
                Id = Guid.NewGuid(),
                AssetsStateId = id,
                Symbol = p.Symbol.Value,
                SymbolId = p.SymbolId,
                AvailableQuantity = (double)p.AvailableQuantity,
                AverageEntryPrice = (double)p.AverageEntryPrice,
                MarketValue = (double)p.MarketValue,
                Quantity = (double)p.Quantity
            }).ToList()
        };
    }

    public static AssetsState FromEntity(AssetsStateEntity entity)
    {
        return new AssetsState(new Assets
        {
            EquityValue = (decimal)entity.EquityValue,
            Cash = new Cash
            {
                MainCurrency = entity.MainCurrency,
                AvailableAmount = (decimal)entity.AvailableCash
            },
            Positions = entity.HeldPositions.Select(p => new Position
            {
                Symbol = new TradingSymbol(p.Symbol),
                SymbolId = p.SymbolId,
                AvailableQuantity = (decimal)p.AvailableQuantity,
                AverageEntryPrice = (decimal)p.AverageEntryPrice,
                MarketValue = (decimal)p.MarketValue,
                Quantity = (decimal)p.Quantity
            }).ToDictionary(p => p.Symbol)
        }, DateTimeOffset.FromUnixTimeMilliseconds(entity.CreationTimestamp));
    }
}
