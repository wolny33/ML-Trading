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


def wait_for_backtest(backtest_id: str, quiet=False):
    def get_state():
        response = requests.get(f"http://localhost:5000/api/backtests/{backtest_id}",
                                auth=HTTPBasicAuth("admin", "password"))
        response.raise_for_status()
        return response.json()["state"]

    if not quiet:
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


class Investment:
    def __init__(self, symbol, start, end, entry, exit_value):
        self.symbol = symbol
        self.start = start
        self.end = end
        self.entry = entry
        self.exit_value = exit_value

    @staticmethod
    def from_position(position, start, end):
        return Investment(position.symbol, start, end, position.averageEntryPrice * position.quantity,
                          position.marketValue)


def get_investments(assets_states):
    def get_last_positions(assets_states):
        result = []
        for before, after in zip(assets_states[:-1], assets_states[1:]):
            today = before.createdAt.date()
            for position in before.assets.positions:

                if position.symbol in [p.symbol for p in after.assets.positions]:
                    continue

                result.append((position, today))

        for position in assets_states[-1].assets.positions:
            result.append((position, assets_states[-1].createdAt.date()))

        return result

    def get_open_dates(assets_states):
        result = dict()
        for before, after in zip(assets_states[:-1], assets_states[1:]):
            today = before.createdAt.date()
            for position in after.assets.positions:

                if position.symbol in [p.symbol for p in before.assets.positions]:
                    continue

                if position.symbol not in result:
                    result[position.symbol] = []

                result[position.symbol].append(today)

        return result

    open_dates = get_open_dates(assets_states)
    last_positions = get_last_positions(assets_states)

    return list(sorted([
        Investment.from_position(last, [v for v in open_dates[last.symbol] if v < end][-1], end) for last, end in
        last_positions
    ], key=lambda i: i.exit_value / i.entry, reverse=True))


class BacktestAnalysis:
    def __init__(self, total_return, investments_count, avg_investment_length, positive_investment_ratio, investments,
                 portfolio_states):
        self.total_return = total_return
        self.investments_count = investments_count
        self.avg_investment_length = avg_investment_length
        self.positive_investment_ratio = positive_investment_ratio
        self.investments = investments
        self.portfolio_states = portfolio_states


def analyze_backtest(backtest_id):
    portfolio_states = fetch_portfolio_states(backtest_id)
    investments = get_investments(portfolio_states)
    test_return = portfolio_states[-1].assets.equityValue / portfolio_states[0].assets.equityValue - 1

    return BacktestAnalysis(
        total_return=test_return,
        investments_count=len(investments),
        avg_investment_length=sum((inv.end - inv.start).days for inv in investments) / len(investments) if len(
            investments) > 0 else 0,
        positive_investment_ratio=len([i for i in investments if i.exit_value / i.entry > 1]) / len(investments) if len(
            investments) > 0 else 0,
        investments=investments,
        portfolio_states=portfolio_states
    )


def get_backtest_ids():
    response = requests.get(f"http://localhost:5000/api/backtests",
                            auth=HTTPBasicAuth("admin", "password"))
    response.raise_for_status()

    return {backtest["description"]: backtest["id"] for backtest in response.json() if backtest["state"] == "Finished"}
