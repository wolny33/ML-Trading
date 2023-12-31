﻿using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using TradingBot.Configuration;
using TradingBot.Exceptions;
using TradingBot.Services;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class TradingTaskExecutorTests
{
    private readonly IActionExecutor _actionExecutor;
    private readonly IExchangeCalendar _calendar;
    private readonly ICurrentTradingTask _currentTradingTask;
    private readonly IInvestmentConfigService _investmentConfig;
    private readonly TradingTaskExecutor _taskExecutor;

    public TradingTaskExecutorTests()
    {
        _actionExecutor = Substitute.For<IActionExecutor>();

        _investmentConfig = Substitute.For<IInvestmentConfigService>();
        _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(new InvestmentConfiguration { Enabled = true });

        _calendar = Substitute.For<IExchangeCalendar>();
        _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>()).Returns(true);

        _currentTradingTask = Substitute.For<ICurrentTradingTask>();
        var logger = Substitute.For<ILogger>();

        _taskExecutor =
            new TradingTaskExecutor(_actionExecutor, _calendar, _investmentConfig, _currentTradingTask, logger);
    }

    [Fact]
    public async Task ShouldSuccessfullyExecuteTradingTask()
    {
        await _taskExecutor.ExecuteAsync();

        Received.InOrder(() =>
        {
            _currentTradingTask.StartAsync(Arg.Any<CancellationToken>());
            _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>());
            _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>());
            _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
            _currentTradingTask.FinishSuccessfullyAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task ShouldNotExecuteTaskIfInvestmentIsDisabled()
    {
        _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(new InvestmentConfiguration { Enabled = false });

        await _taskExecutor.ExecuteAsync();

        Received.InOrder(() =>
        {
            _currentTradingTask.StartAsync(Arg.Any<CancellationToken>());
            _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>());
            _currentTradingTask.MarkAsDisabledFromConfigAsync(Arg.Any<CancellationToken>());
        });

        await _actionExecutor.DidNotReceive().ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldExecuteTaskInBacktestIfInvestmentIsDisabled()
    {
        _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(new InvestmentConfiguration { Enabled = false });
        _currentTradingTask.CurrentBacktestId.Returns(Guid.NewGuid());

        await _taskExecutor.ExecuteAsync();

        Received.InOrder(() =>
        {
            _currentTradingTask.StartAsync(Arg.Any<CancellationToken>());
            _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>());
            _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
            _currentTradingTask.FinishSuccessfullyAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task ShouldNotExecuteTaskIfExchangeDoesNotOpenSoon()
    {
        _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>()).Returns(false);

        await _taskExecutor.ExecuteAsync();

        Received.InOrder(() =>
        {
            _currentTradingTask.StartAsync(Arg.Any<CancellationToken>());
            _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>());
            _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>());
            _currentTradingTask.MarkAsExchangeClosedAsync(Arg.Any<CancellationToken>());
        });

        await _actionExecutor.DidNotReceive().ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldCatchAndSaveResponseExceptions()
    {
        var exception =
            new UnsuccessfulAlpacaResponseException(StatusCodes.Status401Unauthorized, "invalid credentials");
        _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        await _taskExecutor.ExecuteAsync();

        Received.InOrder(() =>
        {
            _currentTradingTask.StartAsync(Arg.Any<CancellationToken>());
            _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>());
            _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>());
            _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
            _currentTradingTask.MarkAsErroredAsync(
                Arg.Is<Error>(e => e.Code == exception.GetError().Code && e.Message == exception.GetError().Message),
                Arg.Any<CancellationToken>());
        });

        await _currentTradingTask.DidNotReceive().FinishSuccessfullyAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldCatchAndSaveUnknownExceptions()
    {
        var exception = new InvalidOperationException("something went wrong");
        _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        await _taskExecutor.ExecuteAsync();

        Received.InOrder(() =>
        {
            _currentTradingTask.StartAsync(Arg.Any<CancellationToken>());
            _investmentConfig.GetConfigurationAsync(Arg.Any<CancellationToken>());
            _calendar.DoesTradingOpenInNext24HoursAsync(Arg.Any<CancellationToken>());
            _actionExecutor.ExecuteTradingActionsAsync(Arg.Any<CancellationToken>());
            _currentTradingTask.MarkAsErroredAsync(
                Arg.Is<Error>(e => e.Code == "unknown" && e.Message == exception.Message),
                Arg.Any<CancellationToken>());
        });

        await _currentTradingTask.DidNotReceive().FinishSuccessfullyAsync(Arg.Any<CancellationToken>());
    }
}
