using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Quartz;
using Serilog;
using TradingBot.Configuration;
using TradingBot.Exceptions;
using TradingBot.Services;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class TradingTaskJobTests
{
    private readonly IActionExecutor _actionExecutor;
    private readonly IExchangeCalendar _calendar;
    private readonly IInvestmentConfigService _investmentConfig;
    private readonly TradingTaskJob _tradingTaskJob;
    private readonly ITradingTaskDetailsUpdater _tradingTaskUpdater;

    public TradingTaskJobTests()
    {
        _actionExecutor = Substitute.For<IActionExecutor>();

        _investmentConfig = Substitute.For<IInvestmentConfigService>();
        _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(new InvestmentConfiguration { Enabled = true });

        _calendar = Substitute.For<IExchangeCalendar>();
        _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>()).Returns(true);

        _tradingTaskUpdater = Substitute.For<ITradingTaskDetailsUpdater>();
        var logger = Substitute.For<ILogger>();

        _tradingTaskJob =
            new TradingTaskJob(_actionExecutor, _investmentConfig, _calendar, _tradingTaskUpdater, logger);
    }

    [Fact]
    public async Task ShouldSuccessfullyExecuteTradingTask()
    {
        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);

        await _tradingTaskJob.Execute(context);

        Received.InOrder(() =>
        {
            _tradingTaskUpdater.StartNewAsync(Arg.Any<CancellationToken>());
            _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>());
            _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>());
            _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
            _tradingTaskUpdater.FinishSuccessfullyAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task ShouldNotExecuteTaskIfInvestmentIsDisabled()
    {
        _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(new InvestmentConfiguration { Enabled = false });

        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);

        await _tradingTaskJob.Execute(context);

        Received.InOrder(() =>
        {
            _tradingTaskUpdater.StartNewAsync(Arg.Any<CancellationToken>());
            _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>());
            _tradingTaskUpdater.MarkAsDisabledFromConfigAsync(Arg.Any<CancellationToken>());
        });

        await _actionExecutor.DidNotReceive().ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldNotExecuteTaskIfExchangeDoesNotOpenSoon()
    {
        _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>()).Returns(false);

        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);

        await _tradingTaskJob.Execute(context);

        Received.InOrder(() =>
        {
            _tradingTaskUpdater.StartNewAsync(Arg.Any<CancellationToken>());
            _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>());
            _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>());
            _tradingTaskUpdater.MarkAsExchangeClosedAsync(Arg.Any<CancellationToken>());
        });

        await _actionExecutor.DidNotReceive().ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldCatchAndSaveResponseExceptions()
    {
        var exception =
            new UnsuccessfulAlpacaResponseException(StatusCodes.Status401Unauthorized, "invalid credentials");
        _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);

        await _tradingTaskJob.Execute(context);

        Received.InOrder(() =>
        {
            _tradingTaskUpdater.StartNewAsync(Arg.Any<CancellationToken>());
            _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>());
            _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>());
            _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
            _tradingTaskUpdater.MarkAsErroredAsync(
                Arg.Is<Error>(e => e.Code == exception.GetError().Code && e.Message == exception.GetError().Message),
                Arg.Any<CancellationToken>());
        });

        await _tradingTaskUpdater.DidNotReceive().FinishSuccessfullyAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldCatchAndSaveUnknownExceptions()
    {
        var exception = new InvalidOperationException("something went wrong");
        _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);

        await _tradingTaskJob.Execute(context);

        Received.InOrder(() =>
        {
            _tradingTaskUpdater.StartNewAsync(Arg.Any<CancellationToken>());
            _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>());
            _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>());
            _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
            _tradingTaskUpdater.MarkAsErroredAsync(
                Arg.Is<Error>(e => e.Code == "unknown" && e.Message == exception.Message),
                Arg.Any<CancellationToken>());
        });

        await _tradingTaskUpdater.DidNotReceive().FinishSuccessfullyAsync(Arg.Any<CancellationToken>());
    }
}
