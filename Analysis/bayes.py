from typing import Any
from sklearn.metrics import make_scorer
from skopt import BayesSearchCV
from skopt.space import Dimension
from backtests import BacktestRequest, start_backtest, wait_for_backtest, get_backtest_return
from strategy import set_strategy_parameters


def run_backtests_with_cv(strategy: str, avg_error: float, folds: int, symbols_per_fold: int) -> float:
    def make_request(fold):
        return BacktestRequest(
            strategy=strategy,
            use_predictor=False,
            avg_prediction_error=avg_error,
            symbols=symbols_per_fold,
            skip=fold*symbols_per_fold
        )

    backtest_returns = []
    for fold in range(folds):
        backtest_id = start_backtest(make_request(fold))

        if not wait_for_backtest(backtest_id, quiet=True):
            backtest_returns.append(-1)
            continue

        backtest_returns.append(get_backtest_return(backtest_id))

    return sum(backtest_returns) / len(backtest_returns)


class SearchWrapper:
    _folds: int = 5
    _symbols: int = 200
    _strategy_name: str | None = None
    _predictor_error: float = 0

    @classmethod
    def set_backtest_config(cls, *, symbols: int, folds: int, strategy_name: str, predictor_error: float) -> None:
        cls._folds = folds
        cls._symbols = symbols
        cls._strategy_name = strategy_name
        cls._predictor_error = predictor_error

    def __init__(self, **kwargs):
        if SearchWrapper._strategy_name is None:
            raise ValueError("strategy_name was not set")

        self.strategy_params = kwargs
        self.backtest_return = None

    def fit(self, train_x, train_y):
        print(f"\tTesting params: {self.strategy_params}")

        set_strategy_parameters(SearchWrapper._strategy_name, self.strategy_params)
        self.backtest_return = run_backtests_with_cv(SearchWrapper._strategy_name, SearchWrapper._predictor_error,
                                                     SearchWrapper._folds, SearchWrapper._symbols)

        return self

    def predict(self, x):
        return self.backtest_return

    def get_params(self, deep=False):
        return self.strategy_params

    def set_params(self, **params):
        return SearchWrapper(**params)


class NullFoldGenerator:
    def __init__(self, n_splits=1):
        self.n_splits = n_splits

    def split(self, X, y, groups=None):
        return [([0], [0]) for _ in range(self.n_splits)]

    def get_n_splits(self, X, y, groups=None):
        return self.n_splits


def perform_bayes_search(strategy_name: str, spaces_dict: dict[str, Dimension], iterations: int, *,
                         prediction_error: float = 0, symbols: int = 1000, folds: int = 0)\
        -> tuple[dict[str, Any], float]:

    SearchWrapper.set_backtest_config(
        strategy_name=strategy_name,
        predictor_error=prediction_error,
        symbols=symbols,
        folds=folds
    )

    bayes_search = BayesSearchCV(
        estimator=SearchWrapper(),
        search_spaces=spaces_dict,
        n_iter=iterations,
        cv=NullFoldGenerator(),
        scoring=make_scorer(lambda expected, predicted: predicted, greater_is_better=True),
        n_jobs=1,
        refit=False,
        return_train_score=False
    )

    bayes_search.fit([0]*5, [0]*5)

    return bayes_search.best_params_, bayes_search.best_score_
