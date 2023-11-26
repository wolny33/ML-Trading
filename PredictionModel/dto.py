from pydantic import BaseModel, validator


class DailyData(BaseModel):
    open: float
    close: float
    high: float
    low: float
    volume: float
    date: str


class PredictRequest(BaseModel):
    data: list[DailyData]

    @validator('data')
    def check_data_length(cls, v):
        if len(v) != 10:  # Replace 'expected_length' with the required length
            raise ValueError('The length of the data list must be exactly 10')
        return v


class DailyPrediction(BaseModel):
    close: float
    high: float
    low: float


class PredictResponse(BaseModel):
    predictions: list[DailyPrediction]
