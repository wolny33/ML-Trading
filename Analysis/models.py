from datetime import datetime
from typing import List
from pydantic import BaseModel


class CashResponse(BaseModel):
    mainCurrency: str
    availableAmount: float  # `decimal` in C# is best represented by `float` in this context
    buyingPower: float


class PositionResponse(BaseModel):
    symbol: str
    quantity: float
    availableQuantity: float
    marketValue: float
    averageEntryPrice: float


class AssetsResponse(BaseModel):
    equityValue: float
    cash: CashResponse
    positions: List[PositionResponse]


class AssetsStateResponse(BaseModel):
    assets: AssetsResponse
    createdAt: datetime


class PortfolioStatesResponse(BaseModel):
    results: list[AssetsStateResponse]
