from pydantic import BaseModel


class BasicStrategyOptionResponse(BaseModel):
    maxStocksBuyCount: int
    minDaysDecreasing: int
    minDaysIncreasing: int
    topGrowingSymbolsBuyRatio: float


class BuyLosersOptionsResponse(BaseModel):
    evaluationFrequencyInDays: int
    analysisLengthInDays: int


class BuyWinnersOptionsResponse(BaseModel):
    evaluationFrequencyInDays: int
    analysisLengthInDays: int
    simultaneousEvaluations: int
    buyWaitTimeInDays: int


class PcaOptionsResponse(BaseModel):
    varianceFraction: float
    analysisLengthInDays: int
    decompositionExpirationInDays: int
    undervaluedThreshold: float
    ignoredThreshold: float
    diverseThreshold: float


class StrategyParametersResponse(BaseModel):
    limitPriceDamping: float
    basic: BasicStrategyOptionResponse
    buyLosers: BuyLosersOptionsResponse
    buyWinners: BuyWinnersOptionsResponse
    pca: PcaOptionsResponse
