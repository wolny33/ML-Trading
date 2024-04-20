import time
import requests
from requests.auth import HTTPBasicAuth
from backtest_models import PortfolioStatesResponse, AssetsStateResponse
from strategy import set_strategy


def fetch_portfolio_states(backtest_id: str) -> list[AssetsStateResponse]:
    full_url = f"http://localhost:5000/api/backtests/{backtest_id}/assets-states"
    response = requests.get(full_url, auth=HTTPBasicAuth("admin", "password"))
    response.raise_for_status()  # Raises an error for bad responses

    validated_response = PortfolioStatesResponse.parse_obj({"results": response.json()})
    return validated_response.results


class BacktestRequest:
    def __init__(self, *, strategy: str, symbols: int, skip: int = 0, use_predictor: bool = False,
                 avg_prediction_error: float = 0):
        self.strategy = strategy
        self.symbols = symbols
        self.skip = skip
        self.use_predictor = use_predictor
        self.avg_prediction_error = avg_prediction_error


def start_backtest(request: BacktestRequest) -> str:
    def make_description(req: BacktestRequest) -> str:
        return f"{req.strategy} ({req.skip}/{req.symbols}): " + \
        ("with predictor" if req.use_predictor else f"{(req.avg_prediction_error * 100):.3f}% mean error")

    set_strategy(request.strategy)

    backtest_response = requests.post("http://localhost:5000/api/backtests", json={
        "start": "2022-03-01",
        "end": "2024-03-01",
        "initialCash": 100_000,
        "shouldUsePredictor": request.use_predictor,
        "meanPredictorError": request.avg_prediction_error,
        "description": make_description(request),
        "symbolSlice": {
            "skip": request.skip,
            "take": request.symbols
        }
    }, auth=HTTPBasicAuth("admin", "password"))

    if backtest_response.status_code == 400:
        print(backtest_response.json())

    backtest_response.raise_for_status()

    return backtest_response.headers.get("location")


def wait_for_backtest(backtest_id: str):
    def get_state():
        response = requests.get(f"http://localhost:5000/api/backtests/{backtest_id}",
                                auth=HTTPBasicAuth("admin", "password"))
        response.raise_for_status()
        return response.json()["state"]

    print(f"Waiting for backtest {backtest_id}")
    while (state := get_state()) == "Running":
        time.sleep(1)

    match state:
        case "Finished":
            return True
        case "Failed":
            return False
        case "Cancelled":
            raise Exception("Backtest was manually cancelled")


def get_backtest_return(backtest_id) -> float | None:
    asset_states = fetch_portfolio_states(backtest_id)

    if len(asset_states) == 0:
        return None

    return asset_states[-1].assets.equityValue / asset_states[0].assets.equityValue - 1
