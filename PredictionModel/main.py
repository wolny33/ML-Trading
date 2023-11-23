from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from keras.models import load_model
import numpy as np

app = FastAPI()
model = load_model('model.h5')


class PredictRequest(BaseModel):
    data: list[float]


class PredictResponse(BaseModel):
    output: list[float]


@app.post("/predict", response_model=PredictResponse)
def predict(request: PredictRequest) -> PredictResponse:
    try:
        input_data = np.array(np.reshape(request.data, (1, 10, 8)))
        prediction = model.predict(input_data)

        return PredictResponse(output=[v for v in prediction.flatten()])
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
