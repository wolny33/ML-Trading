import requests
from pydantic import ValidationError
from models import PortfolioStatesResponse, AssetsStateResponse


def fetch_portfolio_states(backtest_id: str) -> list[AssetsStateResponse]:
    full_url = f"http://yourapi.com/backtest/portfolio-states/{backtest_id}"
    response = requests.get(full_url)
    response.raise_for_status()  # Raises an error for bad responses

    try:
        validated_response = PortfolioStatesResponse.parse_obj(response.json())
        return validated_response.Results
    except ValidationError as e:
        print(f"Validation Error: {e}")
        raise


try:
    portfolio_states = fetch_portfolio_states("some_backtest_id")
    for state in portfolio_states:
        print(f"Assets as of {state.CreatedAt}:")
        print(f"  Equity Value: {state.Assets.EquityValue}")
        print(
            f"  Cash - {state.Assets.Cash.MainCurrency}: Available Amount {state.Assets.Cash.AvailableAmount}, Buying Power: {state.Assets.Cash.BuyingPower}")
        for position in state.Assets.Positions:
            print(f"  Position - {position.Symbol}: Quantity {position.Quantity}, Market Value: {position.MarketValue}")
except Exception as e:
    print(f"Error fetching portfolio states: {e}")
