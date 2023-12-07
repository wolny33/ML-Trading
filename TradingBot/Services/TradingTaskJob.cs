using Quartz;
using TradingBot.Exceptions;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public sealed class TradingTaskJob : IJob
{
    private readonly IActionExecutor _actionExecutor;
    private readonly IExchangeCalendar _calendar;
    private readonly IInvestmentConfigService _investmentConfig;
    private readonly ILogger _logger;
    private readonly ITradingTaskDetailsUpdater _tradingTask;

    public TradingTaskJob(IActionExecutor actionExecutor, IInvestmentConfigService investmentConfig,
        IExchangeCalendar calendar, ITradingTaskDetailsUpdater tradingTask, ILogger logger)
    {
        _actionExecutor = actionExecutor;
        _investmentConfig = investmentConfig;
        _calendar = calendar;
        _tradingTask = tradingTask;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _tradingTask.StartNewAsync(context.CancellationToken);

            if (!(await _investmentConfig.GetConfigurationAsync(context.CancellationToken)).Enabled)
            {
                _logger.Information("Automatic investing is disabled in configuration - no actions were taken");
                await _tradingTask.MarkAsDisabledFromConfigAsync(context.CancellationToken);
                return;
            }

            if (!await _calendar.DoesTradingOpenInNext24HoursAsync(context.CancellationToken))
            {
                _logger.Information("Exchange does not open in the next 24 hours - no actions were taken");
                await _tradingTask.MarkAsExchangeClosedAsync(context.CancellationToken);
                return;
            }

            await _actionExecutor.ExecuteTradingActionsAsync(context.CancellationToken);
            await _tradingTask.FinishSuccessfullyAsync(context.CancellationToken);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Trading task execution failed");
            var error = (e as ResponseException)?.GetError() ?? new Error("unknown", e.Message);
            await _tradingTask.MarkAsErroredAsync(error, context.CancellationToken);
        }
    }

    public static void RegisterJob(IServiceCollectionQuartzConfigurator options)
    {
        var jobKey = new JobKey(nameof(TradingTaskJob));
        options.AddJob<TradingTaskJob>(opts => opts.WithIdentity(jobKey));
        options.AddTrigger(opts =>
            opts.ForJob(jobKey).WithIdentity($"{nameof(TradingTaskJob)}Trigger")
                .WithDailyTimeIntervalSchedule(schedule =>
                    schedule.OnEveryDay().StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(12, 0)).WithRepeatCount(0))
                .StartNow());
    }
}
