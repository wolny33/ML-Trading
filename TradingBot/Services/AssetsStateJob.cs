using Quartz;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public sealed class AssetsStateJob : IJob
{
    private readonly IAssetsStateCommand _command;
    private readonly ILogger _logger;

    public AssetsStateJob(IAssetsStateCommand command, ILogger logger)
    {
        _command = command;
        _logger = logger.ForContext<AssetsStateJob>();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _command.SaveCurrentAssetsAsync(context.CancellationToken);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Assets state saving failed");
        }
    }

    public static void RegisterJob(IServiceCollectionQuartzConfigurator options)
    {
        var jobKey = new JobKey(nameof(AssetsStateJob));
        options.AddJob<AssetsStateJob>(opts => opts.WithIdentity(jobKey));
        options.AddTrigger(opts =>
            opts.ForJob(jobKey).WithIdentity($"{nameof(AssetsStateJob)}Trigger")
                .WithSimpleSchedule(schedule => schedule.WithIntervalInHours(1).RepeatForever())
                .StartNow());
    }
}
