using Quartz;

namespace TradingBot.Services;

public sealed class TradingTaskJob : IJob
{
    private readonly TradingTaskExecutor _tradingTaskExecutor;

    public TradingTaskJob(TradingTaskExecutor tradingTaskExecutor)
    {
        _tradingTaskExecutor = tradingTaskExecutor;
    }

    public Task Execute(IJobExecutionContext context)
    {
        return _tradingTaskExecutor.ExecuteAsync(context.CancellationToken);
    }

    public static void RegisterJob(IServiceCollectionQuartzConfigurator options)
    {
        var jobKey = new JobKey(nameof(TradingTaskJob));
        options.AddJob<TradingTaskJob>(opts => opts.WithIdentity(jobKey));
        options.AddTrigger(opts =>
            opts.ForJob(jobKey).WithIdentity($"{nameof(TradingTaskJob)}Trigger")
                .WithDailyTimeIntervalSchedule(schedule =>
                    schedule.OnEveryDay().StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(12, 0)).WithRepeatCount(-1))
                .StartNow());
    }
}
