using System.Diagnostics.CodeAnalysis;
using Alpaca.Markets;
using Microsoft.AspNetCore.Authentication;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IExchangeCalendar
{
    Task<bool> DoesTradingOpenInNext24HoursAsync(CancellationToken token = default);
}

public sealed class ExchangeCalendar : IExchangeCalendar
{
    private readonly IMarketDataCache _cache;
    private readonly IAlpacaCallQueue _callQueue;
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly ISystemClock _clock;
    private readonly ILogger _logger;
    private readonly ICurrentTradingTask _tradingTask;

    public ExchangeCalendar(ISystemClock clock, IAlpacaClientFactory clientFactory, ILogger logger,
        IAlpacaCallQueue callQueue, ICurrentTradingTask tradingTask, IMarketDataCache cache)
    {
        _clock = clock;
        _clientFactory = clientFactory;
        _callQueue = callQueue;
        _tradingTask = tradingTask;
        _cache = cache;
        _logger = logger.ForContext<ExchangeCalendar>();
    }

    public async Task<bool> DoesTradingOpenInNext24HoursAsync(CancellationToken token = default)
    {
        if (_tradingTask.CurrentBacktestId is not null)
        {
            _logger.Verbose("Backtest is active - checking if there is any symbol data for given day in cache");
            return _cache.GetMostActiveCachedSymbolsForDay(_tradingTask.GetTaskDay()).Any();
        }

        var now = _clock.UtcNow;
        var nextTradingDay = await SendCalendarRequestAsync(now, token);
        if (nextTradingDay is null) return false;

        return nextTradingDay.Trading.OpenEst - now <= TimeSpan.FromDays(1);
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    private async Task<IIntervalCalendar?> SendCalendarRequestAsync(DateTimeOffset now, CancellationToken token)
    {
        using var client = await _clientFactory.CreateTradingClientAsync(token);
        var todayEst = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now.UtcDateTime, GetEstTimeZone()));
        var result =
            await _callQueue.SendRequestWithRetriesAsync(() => client
                    .ListIntervalCalendarAsync(new CalendarRequest(todayEst, todayEst.AddDays(1)), token), _logger)
                .ExecuteWithErrorHandling(_logger);
        return result.AsEnumerable().FirstOrDefault();
    }

    private static TimeZoneInfo GetEstTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
    }
}
