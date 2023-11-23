from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from keras.models import load_model
import numpy as np
from datetime import datetime

app = FastAPI()
model = load_model('model.h5')


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


@app.post("/predict", response_model=PredictResponse)
def predict(request: PredictRequest) -> PredictResponse:
    try:
        input_data = np.vstack([get_features_vector(v) for v in request.data])

        # TODO: scale...

        reshaped = np.reshape(input_data, (1, 10, 8))
        prediction = np.reshape(model.predict(reshaped), (-1, 3))

        # TODO: descale...

        close = [v for v in prediction[:, 0]]
        high = [v for v in prediction[:, 1]]
        low = [v for v in prediction[:, 2]]
        return PredictResponse(close=close, high=high, low=low)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


def get_features_vector(data: DailyData) -> np.ndarray:
    date = datetime.fromisoformat(data.date)
    return np.array([
        data.open,
        data.close,
        data.high,
        data.low,
        data.volume,
        date.weekday() / 6,
        np.sin(2 * np.pi * date.timetuple().tm_yday / 366),
        np.cos(2 * np.pi * date.timetuple().tm_yday / 366)
    ])
