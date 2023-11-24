from datetime import datetime
import numpy as np
from fastapi import FastAPI, HTTPException
from keras.models import load_model
from dto import DailyData, PredictResponse, PredictRequest
from scaler import ScalerCollection


app = FastAPI()
model = load_model("model.h5")
scalers = ScalerCollection.load("config.json")


@app.post("/predict", response_model=PredictResponse)
def predict(request: PredictRequest) -> PredictResponse:
    try:
        input_data = np.vstack([get_features_vector(v) for v in request.data])

        input_data[:, 0] = scalers.open_scaler.scale(input_data[:, 0])
        input_data[:, 1] = scalers.close_scaler.scale(input_data[:, 1])
        input_data[:, 2] = scalers.high_scaler.scale(input_data[:, 2])
        input_data[:, 3] = scalers.low_scaler.scale(input_data[:, 3])
        input_data[:, 4] = scalers.volume_scaler.scale(input_data[:, 4])

        reshaped = np.reshape(input_data, (1, 10, 8))
        prediction = np.reshape(model.predict(reshaped), (-1, 3))

        prediction[:, 0] = scalers.close_scaler.descale(prediction[:, 0])
        prediction[:, 1] = scalers.high_scaler.descale(prediction[:, 1])
        prediction[:, 2] = scalers.low_scaler.descale(prediction[:, 2])

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
