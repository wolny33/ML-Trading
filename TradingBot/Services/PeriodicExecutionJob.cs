using Quartz;

namespace TradingBot.Services;

public sealed class PeriodicExecutionJob : IJob
{
    private readonly IActionExecutor _actionExecutor;
    private readonly IExchangeCalendar _calendar;
    private readonly IInvestmentConfigService _investmentConfig;

    public PeriodicExecutionJob(IActionExecutor actionExecutor, IInvestmentConfigService investmentConfig,
        IExchangeCalendar calendar)
    {
        _actionExecutor = actionExecutor;
        _investmentConfig = investmentConfig;
        _calendar = calendar;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            if (!(await _investmentConfig.GetConfigurationAsync(context.CancellationToken)).Enabled)
                // TODO: Log + save in db
                return;

            if (!await _calendar.DoesTradingOpenInNext24Hours(context.CancellationToken))
                // TODO: Log + save in db
                return;

            await _actionExecutor.ExecuteTradingActionsAsync(context.CancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log + save in db
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
