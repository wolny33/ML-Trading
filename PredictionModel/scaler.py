from __future__ import annotations
import json
from typing import Any, Tuple, Dict


class Scaler:
    """
    Scales data to 0-1 range
    """
    def __init__(self):
        self._eps = 1e-9
        self._min_input = None
        self._max_input = None

    def fit(self, data: Any) -> None:
        min_v = min(data)
        max_v = max(data)
        self._min_input = min_v \
            if self._min_input is None or self._min_input > min_v \
            else self._min_input
        self._max_input = max_v \
            if self._max_input is None or self._max_input < max_v \
            else self._max_input

    def scale(self, data: Any) -> Any:
        return (data - self._min_input) / (self._max_input - self._min_input + self._eps)

    def descale(self, data: Any) -> Any:
        return data * (self._max_input - self._min_input + self._eps) + self._min_input

    def get_range(self) -> Tuple[float, float]:
        return self._min_input, self._max_input

    def to_json(self) -> Dict[str, Any]:
        return {
            "min": self._min_input,
            "max": self._max_input
        }

    @staticmethod
    def from_json(data: Dict[str, Any]) -> Scaler:
        result = Scaler()
        result._min_input = data["min"]
        result._max_input = data["max"]
        return result


class ScalerCollection:
    def __init__(self, json_str: str):
        data = json.loads(json_str)
        self.open_scaler = Scaler.from_json(data["open"])
        self.close_scaler = Scaler.from_json(data["close"])
        self.high_scaler = Scaler.from_json(data["high"])
        self.low_scaler = Scaler.from_json(data["low"])
        self.volume_scaler = Scaler.from_json(data["volume"])
        self.fear_greed_index_scaler = Scaler.from_json(data["fear_greed_index"])

    @staticmethod
    def load(file_name: str) -> ScalerCollection:
        with open(file_name, mode="r") as file:
            json_str = file.read()
            return ScalerCollection(json_str)
