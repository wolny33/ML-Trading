using TradingBot.Exceptions;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public sealed class TradingTaskExecutor
{
    private readonly IActionExecutor _actionExecutor;
    private readonly IExchangeCalendar _calendar;
    private readonly IInvestmentConfigService _investmentConfig;
    private readonly ILogger _logger;
    private readonly ITradingTaskDetailsUpdater _tradingTask;

    public TradingTaskExecutor(IActionExecutor actionExecutor, IExchangeCalendar calendar,
        IInvestmentConfigService investmentConfig, ITradingTaskDetailsUpdater tradingTask, ILogger logger)
    {
        _actionExecutor = actionExecutor;
        _calendar = calendar;
        _investmentConfig = investmentConfig;
        _tradingTask = tradingTask;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken token = default)
    {
        try
        {
            await _tradingTask.StartNewAsync(token);

            if (!(await _investmentConfig.GetConfigurationAsync(token)).Enabled)
            {
                _logger.Information("Automatic investing is disabled in configuration - no actions were taken");
                await _tradingTask.MarkAsDisabledFromConfigAsync(token);
                return;
            }

            if (!await _calendar.DoesTradingOpenInNext24HoursAsync(token))
            {
                _logger.Information("Exchange does not open in the next 24 hours - no actions were taken");
                await _tradingTask.MarkAsExchangeClosedAsync(token);
                return;
            }

            await _actionExecutor.ExecuteTradingActionsAsync(token);
            await _tradingTask.FinishSuccessfullyAsync(token);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Trading task execution failed");
            var error = (e as ResponseException)?.GetError() ?? new Error("unknown", e.Message);
            await _tradingTask.MarkAsErroredAsync(error, token);
        }
    }
}
