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
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly ISystemClock _clock;
    private readonly ILogger _logger;

    public ExchangeCalendar(ISystemClock clock, IAlpacaClientFactory clientFactory, ILogger logger)
    {
        _clock = clock;
        _clientFactory = clientFactory;
        _logger = logger.ForContext<ExchangeCalendar>();
    }

    public async Task<bool> DoesTradingOpenInNext24HoursAsync(CancellationToken token = default)
    {
        var now = _clock.UtcNow;
        var nextTradingDay = await SendCalendarRequestAsync(now, token);
        if (nextTradingDay is null) return false;

        return nextTradingDay.Trading.OpenEst - now <= TimeSpan.FromDays(1);
    }

    private async Task<IIntervalCalendar?> SendCalendarRequestAsync(DateTimeOffset now, CancellationToken token)
    {
        using var client = await _clientFactory.CreateTradingClientAsync(token);
        var todayEst = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now.UtcDateTime, GetEstTimeZone()));
        var result =
            await client.ListIntervalCalendarAsync(new CalendarRequest(todayEst, todayEst.AddDays(1)), token)
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
