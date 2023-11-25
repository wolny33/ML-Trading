from datetime import datetime
import numpy as np
from fastapi import FastAPI, HTTPException
from keras.models import load_model
from dto import DailyData, PredictResponse, PredictRequest, DailyPrediction, HealthResponse
from scaler import ScalerCollection


app = FastAPI()
model = load_model("model.h5")
scalers = ScalerCollection.load("config.json")


@app.post("/predict", response_model=PredictResponse)
def predict(request: PredictRequest) -> PredictResponse:
    try:
        input_data = np.vstack([get_features_vector(v) for v in request.data])
        scaled = scale_input(input_data, scalers)
        reshaped = np.reshape(scaled, (1, 10, 8))

        output = np.reshape(model.predict(reshaped), (-1, 3))

        prediction = descale_output(output, scalers)

        return PredictResponse(predictions=[DailyPrediction(close=v[0], high=v[1], low=v[2]) for v in prediction])
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    return HealthResponse(status="healthy")


def get_features_vector(data: DailyData) -> np.ndarray:
    date = datetime.fromisoformat(data.date)
    return np.array([
        data.open,
        data.close,
        data.high,
        data.low,
        np.log(data.volume),
        date.weekday() / 6,
        np.sin(2 * np.pi * date.timetuple().tm_yday / 366),
        np.cos(2 * np.pi * date.timetuple().tm_yday / 366)
    ])


def scale_input(input_data: np.ndarray, scaler_collection: ScalerCollection) -> np.ndarray:
    result = input_data.copy()
    result[:, 0] = scaler_collection.open_scaler.scale(result[:, 0])
    result[:, 1] = scaler_collection.close_scaler.scale(result[:, 1])
    result[:, 2] = scaler_collection.high_scaler.scale(result[:, 2])
    result[:, 3] = scaler_collection.low_scaler.scale(result[:, 3])
    result[:, 4] = scaler_collection.volume_scaler.scale(result[:, 4])
    return result


def descale_output(output: np.ndarray, scaler_collection: ScalerCollection) -> np.ndarray:
    result = output.copy()
    result[:, 0] = scaler_collection.close_scaler.descale(result[:, 0])
    result[:, 1] = scaler_collection.high_scaler.descale(result[:, 1])
    result[:, 2] = scaler_collection.low_scaler.descale(result[:, 2])
    return result
