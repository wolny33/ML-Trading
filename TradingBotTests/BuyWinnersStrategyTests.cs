using NSubstitute;
using TradingBot.Services;
using TradingBot.Services.Strategy;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class BuyWinnersStrategyTests
{
    private readonly ITradingActionQuery _actionQuery;
    private readonly IAssetsDataSource _assets;
    private readonly IMarketDataSource _marketData;
    private readonly IBuyWinnersStrategyStateService _stateService;
    private readonly BuyWinnersStrategy _strategy;

    public BuyWinnersStrategyTests()
    {
        _assets = Substitute.For<IAssetsDataSource>();
        _marketData = Substitute.For<IMarketDataSource>();
        _stateService = Substitute.For<IBuyWinnersStrategyStateService>();
        _actionQuery = Substitute.For<ITradingActionQuery>();

        var tradingTask = Substitute.For<ICurrentTradingTask>();
        tradingTask.GetTaskDay().Returns(new DateOnly(2024, 3, 10));
        tradingTask.GetTaskTime().Returns(new DateTimeOffset(2024, 3, 10, 12, 0, 0, TimeSpan.Zero));
        tradingTask.CurrentBacktestId.Returns((Guid?)null);

        _strategy = new BuyWinnersStrategy(tradingTask, _stateService, _marketData, _assets, _actionQuery);
    }

    [Fact]
    public async Task ShouldBuyPendingSymbolsIfThereIsWaitingEvaluation()
    {
        await Task.FromException(new NotImplementedException());
    }

    [Fact]
    public async Task ShouldNotBuySymbolsIfEvaluationIsLessThanSevenDaysOld()
    {
        await Task.FromException(new NotImplementedException());
    }

    [Fact]
    public async Task ShouldDoNothingIfItIsNotEvaluationDayAndThereAreNoPendingEvaluations()
    {
        await Task.FromException(new NotImplementedException());
    }

    [Fact]
    public async Task ShouldCreateNewEvaluationAndSellExpiredOnesOnEvaluationDay()
    {
        await Task.FromException(new NotImplementedException());
    }

    [Fact]
    public async Task ShouldPerformEvaluationOnFirstDay()
    {
        await Task.FromException(new NotImplementedException());
    }

    [Fact]
    public async Task ShouldNotUseUpAllMoneyIfThereAreLessThanThreeActiveEvaluations()
    {
        await Task.FromException(new NotImplementedException());
    }
}