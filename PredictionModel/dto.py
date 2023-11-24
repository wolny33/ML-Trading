from pydantic import BaseModel


class DailyData(BaseModel):
    open: float
    close: float
    high: float
    low: float
    volume: float
    date: str


class PredictRequest(BaseModel):
    data: list[DailyData]


class PredictResponse(BaseModel):
    close: list[float]
    high: list[float]
    low: list[float]
