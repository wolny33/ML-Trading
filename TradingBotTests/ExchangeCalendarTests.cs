﻿using System.Reflection;
using Alpaca.Markets;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using NSubstitute;
using Serilog;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class ExchangeCalendarTests : IAsyncDisposable
{
    private readonly ExchangeCalendar _calendar;
    private readonly IMarketDataCache _marketDataCache;
    private readonly DateTimeOffset _now = new(2023, 12, 7, 13, 14, 0, TimeSpan.Zero);
    private readonly IAlpacaTradingClient _tradingClient;
    private readonly ICurrentTradingTask _tradingTask;

    public ExchangeCalendarTests()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(_now);

        _tradingClient = Substitute.For<IAlpacaTradingClient>();
        var clientFactory = Substitute.For<IAlpacaClientFactory>();
        clientFactory.CreateTradingClientAsync(Arg.Any<CancellationToken>()).Returns(_tradingClient);

        _tradingTask = Substitute.For<ICurrentTradingTask>();
        _tradingTask.CurrentBacktestId.Returns((Guid?)null);

        var logger = Substitute.For<ILogger>();
        var callQueue = new CallQueueMock();
        _marketDataCache = Substitute.For<IMarketDataCache>();

        _calendar = new ExchangeCalendar(clock, clientFactory, logger, callQueue, _tradingTask, _marketDataCache);
    }

    public ValueTask DisposeAsync()
    {
        return _calendar.DisposeAsync();
    }

    [Fact]
    public async Task ShouldReturnTrueIfExchangeIsOpen()
    {
        _tradingClient
            .ListIntervalCalendarAsync(
                Arg.Is<CalendarRequest>(r =>
                    r.DateInterval.From == new DateOnly(2023, 12, 7) &&
                    r.DateInterval.Into == new DateOnly(2023, 12, 8)), Arg.Any<CancellationToken>()).Returns(new[]
            {
                IntervalCalendar.WithTradingInterval(new DateOnly(2023, 12, 7), new TimeOnly(8, 0, 0),
                    new TimeOnly(17, 0, 0)),
                IntervalCalendar.WithTradingInterval(new DateOnly(2023, 12, 8), new TimeOnly(8, 0, 0),
                    new TimeOnly(17, 0, 0))
            });

        (await _calendar.DoesTradingOpenInNext24HoursAsync()).Should().BeTrue();

        await _tradingClient.Received(1).ListIntervalCalendarAsync(
            Arg.Is<CalendarRequest>(r =>
                r.DateInterval.From == new DateOnly(2023, 12, 7) && r.DateInterval.Into == new DateOnly(2023, 12, 8)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnTrueIfExchangeOpensToday()
    {
        _tradingClient
            .ListIntervalCalendarAsync(
                Arg.Is<CalendarRequest>(r =>
                    r.DateInterval.From == new DateOnly(2023, 12, 7) &&
                    r.DateInterval.Into == new DateOnly(2023, 12, 8)), Arg.Any<CancellationToken>()).Returns(new[]
            {
                IntervalCalendar.WithTradingInterval(new DateOnly(2023, 12, 7), new TimeOnly(9, 0, 0),
                    new TimeOnly(17, 0, 0)),
                IntervalCalendar.WithTradingInterval(new DateOnly(2023, 12, 8), new TimeOnly(9, 0, 0),
                    new TimeOnly(17, 0, 0))
            });

        (await _calendar.DoesTradingOpenInNext24HoursAsync()).Should().BeTrue();

        await _tradingClient.Received(1).ListIntervalCalendarAsync(
            Arg.Is<CalendarRequest>(r =>
                r.DateInterval.From == new DateOnly(2023, 12, 7) && r.DateInterval.Into == new DateOnly(2023, 12, 8)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnTrueIfExchangeOpensTomorrow()
    {
        _tradingClient
            .ListIntervalCalendarAsync(
                Arg.Is<CalendarRequest>(r =>
                    r.DateInterval.From == new DateOnly(2023, 12, 7) &&
                    r.DateInterval.Into == new DateOnly(2023, 12, 8)), Arg.Any<CancellationToken>()).Returns(new[]
            {
                IntervalCalendar.WithTradingInterval(new DateOnly(2023, 12, 8), new TimeOnly(8, 0, 0),
                    new TimeOnly(17, 0, 0))
            });

        (await _calendar.DoesTradingOpenInNext24HoursAsync()).Should().BeTrue();

        await _tradingClient.Received(1).ListIntervalCalendarAsync(
            Arg.Is<CalendarRequest>(r =>
                r.DateInterval.From == new DateOnly(2023, 12, 7) && r.DateInterval.Into == new DateOnly(2023, 12, 8)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFalseIfExchangeOpensInMoreThan24Hours()
    {
        _tradingClient
            .ListIntervalCalendarAsync(
                Arg.Is<CalendarRequest>(r =>
                    r.DateInterval.From == new DateOnly(2023, 12, 7) &&
                    r.DateInterval.Into == new DateOnly(2023, 12, 8)), Arg.Any<CancellationToken>()).Returns(new[]
            {
                IntervalCalendar.WithTradingInterval(new DateOnly(2023, 12, 8), new TimeOnly(9, 0, 0),
                    new TimeOnly(17, 0, 0))
            });

        (await _calendar.DoesTradingOpenInNext24HoursAsync()).Should().BeFalse();

        await _tradingClient.Received(1).ListIntervalCalendarAsync(
            Arg.Is<CalendarRequest>(r =>
                r.DateInterval.From == new DateOnly(2023, 12, 7) && r.DateInterval.Into == new DateOnly(2023, 12, 8)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFalseIfExchangeDoesNotOpenTomorrow()
    {
        _tradingClient
            .ListIntervalCalendarAsync(
                Arg.Is<CalendarRequest>(r =>
                    r.DateInterval.From == new DateOnly(2023, 12, 7) &&
                    r.DateInterval.Into == new DateOnly(2023, 12, 8)), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IIntervalCalendar>());

        (await _calendar.DoesTradingOpenInNext24HoursAsync()).Should().BeFalse();

        await _tradingClient.Received(1).ListIntervalCalendarAsync(
            Arg.Is<CalendarRequest>(r =>
                r.DateInterval.From == new DateOnly(2023, 12, 7) && r.DateInterval.Into == new DateOnly(2023, 12, 8)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnTrueInBacktestIfThereIsCachedSymbolDataForNextDay()
    {
        _tradingTask.CurrentBacktestId.Returns(Guid.NewGuid());
        _tradingTask.GetTaskDay().Returns(new DateOnly(2024, 1, 1));
        _marketDataCache.GetMostActiveCachedSymbolsForDay(new DateOnly(2024, 1, 2))
            .Returns(new[] { new TradingSymbol("TKN") });

        (await _calendar.DoesTradingOpenInNext24HoursAsync()).Should().BeTrue();

        await _tradingClient.DidNotReceive()
            .ListIntervalCalendarAsync(Arg.Any<CalendarRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFalseInBacktestIfThereIsNoCachedSymbolDataForNextDay()
    {
        _tradingTask.CurrentBacktestId.Returns(Guid.NewGuid());
        _tradingTask.GetTaskDay().Returns(new DateOnly(2024, 1, 1));
        _marketDataCache.GetMostActiveCachedSymbolsForDay(new DateOnly(2024, 1, 2))
            .Returns(Enumerable.Empty<TradingSymbol>());

        (await _calendar.DoesTradingOpenInNext24HoursAsync()).Should().BeFalse();

        await _tradingClient.DidNotReceive()
            .ListIntervalCalendarAsync(Arg.Any<CalendarRequest>(), Arg.Any<CancellationToken>());
    }

    private sealed record IntervalCalendar(OpenClose Trading, OpenClose Session) : IIntervalCalendar
    {
        public static IntervalCalendar WithTradingInterval(DateOnly date, TimeOnly open, TimeOnly close)
        {
            var openClose = (OpenClose)Activator.CreateInstance(typeof(OpenClose),
                BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { date, open, close }, null)!;
            return new IntervalCalendar(openClose, openClose);
        }
    }
}
