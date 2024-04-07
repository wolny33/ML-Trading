import requests
from requests.auth import HTTPBasicAuth
from models import PortfolioStatesResponse, AssetsStateResponse


def fetch_portfolio_states(backtest_id: str) -> list[AssetsStateResponse]:
    full_url = f"http://localhost:5000/api/backtests/{backtest_id}/assets-states"
    response = requests.get(full_url, auth=HTTPBasicAuth("admin", "password"))
    response.raise_for_status()  # Raises an error for bad responses

    validated_response = PortfolioStatesResponse.parse_obj({"results": response.json()})
    return validated_response.results
