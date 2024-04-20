from sklearn.metrics import make_scorer
from skopt import BayesSearchCV
from strategy import *
from backtests import *


class SearchWrapper:
    _skip = 0
    _symbols = 1000
    _strategy_name = None
    _predictor_error = 0

    @classmethod
    def set_backtest_config(cls, *, symbols, skip, strategy_name, predictor_error):
        cls._skip = skip
        cls._symbols = symbols
        cls._strategy_name = strategy_name
        cls._predictor_error = predictor_error

    def __init__(self, **kwargs):
        if SearchWrapper._strategy_name is None:
            raise ValueError("strategy_name was not set")

        self.strategy_params = kwargs
        self.backtest_return = None

    def fit(self, train_x, train_y):
        print(f"Running a backtest with params: {self.strategy_params}")

        set_strategy_parameters(SearchWrapper._strategy_name, self.strategy_params)
        backtest_id = start_backtest(BacktestRequest(
            strategy=SearchWrapper._strategy_name,
            use_predictor=False,
            avg_prediction_error=SearchWrapper._predictor_error,
            skip=SearchWrapper._skip,
            symbols=SearchWrapper._symbols
        ))

        if not wait_for_backtest(backtest_id):
            self.backtest_return = -1
        else:
            self.backtest_return = get_backtest_return(backtest_id)

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


def perform_bayes_search(strategy_name, spaces_dict, iterations, *, prediction_error=0, symbols=1000, skip=0):

    SearchWrapper.set_backtest_config(
        strategy_name=strategy_name,
        predictor_error=prediction_error,
        symbols=symbols,
        skip=skip
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
