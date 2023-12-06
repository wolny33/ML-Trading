using Quartz;
using TradingBot.Exceptions;

namespace TradingBot.Services;

public sealed class PeriodicExecutionJob : IJob
{
    private readonly IActionExecutor _actionExecutor;
    private readonly IExchangeCalendar _calendar;
    private readonly IInvestmentConfigService _investmentConfig;
    private readonly ITradingTaskDetailsUpdater _tradingTask;

    public PeriodicExecutionJob(IActionExecutor actionExecutor, IInvestmentConfigService investmentConfig,
        IExchangeCalendar calendar, ITradingTaskDetailsUpdater tradingTask)
    {
        _actionExecutor = actionExecutor;
        _investmentConfig = investmentConfig;
        _calendar = calendar;
        _tradingTask = tradingTask;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _tradingTask.StartNewAsync(context.CancellationToken);

            if (!(await _investmentConfig.GetConfigurationAsync(context.CancellationToken)).Enabled)
            {
                // TODO: Log
                await _tradingTask.MarkAsDisabledFromConfigAsync(context.CancellationToken);
                return;
            }

            if (!await _calendar.DoesTradingOpenInNext24Hours(context.CancellationToken))
            {
                // TODO: Log
                await _tradingTask.MarkAsExchangeClosedAsync(context.CancellationToken);
                return;
            }

            await _actionExecutor.ExecuteTradingActionsAsync(context.CancellationToken);
            await _tradingTask.FinishSuccessfullyAsync(context.CancellationToken);
        }
        catch (Exception e)
        {
            // TODO: Log
            var error = (e as ResponseException)?.GetError() ?? new Error("unknown", e.Message);
            await _tradingTask.MarkAsErroredAsync(error, context.CancellationToken);
        }
    }

    public static void RegisterJob(IServiceCollectionQuartzConfigurator options)
    {
        var jobKey = new JobKey(nameof(PeriodicExecutionJob));
        options.AddJob<PeriodicExecutionJob>(opts => opts.WithIdentity(jobKey));
        options.AddTrigger(opts =>
            opts.ForJob(jobKey).WithIdentity($"{nameof(PeriodicExecutionJob)}Trigger")
                .WithDailyTimeIntervalSchedule(schedule =>
                    schedule.OnEveryDay().StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(12, 0)).WithRepeatCount(0))
                .StartNow());
    }
}
