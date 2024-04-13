from typing import Any
import requests
from requests.auth import HTTPBasicAuth
from strategy_models import StrategyParametersResponse


def get_strategy_names() -> list[str]:
    return [
        "Basic strategy",
        "Greedy optimal strategy",
        "Overreaction strategy",
        "Overreaction strategy with predictions",
        "Trend following strategy",
        "Trend following strategy with predictions",
        "PCA strategy",
        "PCA strategy with predictions"
    ]


def _get_parameters_field_name(strategy_name: str) -> str | None:
    match strategy_name:
        case "Basic strategy":
            return "basic"
        case "Greedy optimal strategy":
            return None
        case "Overreaction strategy":
            return "buyLosers"
        case "Overreaction strategy with predictions":
            return "buyLosers"
        case "Trend following strategy":
            return "buyWinners"
        case "Trend following strategy with predictions":
            return "buyWinners"
        case "PCA strategy":
            return "pca"
        case "PCA strategy with predictions":
            return "pca"


def set_strategy(strategy_name: str) -> None:
    strategy_response = requests.put("http://localhost:5000/api/strategy/selection", json={
        "name": strategy_name
    }, auth=HTTPBasicAuth("admin", "password"))
    strategy_response.raise_for_status()


def get_strategy_parameters() -> StrategyParametersResponse:
    response = requests.get("http://localhost:5000/api/strategy", auth=HTTPBasicAuth("admin", "password"))
    response.raise_for_status()

    parsed = StrategyParametersResponse.parse_obj(response.json())
    return parsed


def set_strategy_parameters(strategy_name: str, parameters: dict[str, Any]) -> None:
    current = get_strategy_parameters()
    parameters_dict = current.model_dump()

    parameters_request = {
        "limitPriceDamping": parameters["limitPriceDamping"]
        if "limitPriceDamping" in parameters else current.limitPriceDamping,
        "basic": parameters_dict["basic"],
        "buyLosers": parameters_dict["buyLosers"],
        "buyWinners": parameters_dict["buyWinners"],
        "pca": parameters_dict["pca"]
    }

    if section_name := _get_parameters_field_name(strategy_name) is not None:
        parameters_dict[strategy_name] = {k: v for k, v in parameters.items() if not k == "limitPriceDamping"}

    response = requests.put("http://localhost:5000/api/strategy", json=parameters_request,
                            auth=HTTPBasicAuth("admin", "password"))
    response.raise_for_status()
