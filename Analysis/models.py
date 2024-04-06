from datetime import datetime
from typing import List
from pydantic import BaseModel


class CashResponse(BaseModel):
    MainCurrency: str
    AvailableAmount: float  # `decimal` in C# is best represented by `float` in this context
    BuyingPower: float


class PositionResponse(BaseModel):
    Symbol: str
    Quantity: float
    AvailableQuantity: float
    MarketValue: float
    AverageEntryPrice: float


class AssetsResponse(BaseModel):
    EquityValue: float
    Cash: CashResponse
    Positions: List[PositionResponse]


class AssetsStateResponse(BaseModel):
    Assets: AssetsResponse
    CreatedAt: datetime


class PortfolioStatesResponse(BaseModel):
    Results: list[AssetsStateResponse]
