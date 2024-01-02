using System.Threading.Channels;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IAlpacaCallQueue
{
    Task<T> SendRequestWithRetriesAsync<T>(Func<Task<T>> request, ILogger? logger = null) where T : class;
}

public sealed class AlpacaCallQueue : IAlpacaCallQueue, IAsyncDisposable
{
    private readonly Func<TimeSpan, Task> _delay;
    private readonly ILogger _logger;
    private readonly Channel<QueuedAlpacaCall> _queuedCalls = Channel.CreateUnbounded<QueuedAlpacaCall>();
    private readonly Task _queueProcessingTask;
    private bool _hasCallsRemaining = true;

    public AlpacaCallQueue(ILogger logger) : this(logger, Task.Delay)
    {
    }

    // This constructor allows controlling time-related functionalities in unit tests
    internal AlpacaCallQueue(ILogger logger, Func<TimeSpan, Task> delay)
    {
        _delay = delay;
        _logger = logger.ForContext<AlpacaCallQueue>();
        _queueProcessingTask = ProcessQueueAsync();
    }

    public async Task<T> SendRequestWithRetriesAsync<T>(Func<Task<T>> request, ILogger? logger = null) where T : class
    {
        if (_hasCallsRemaining && await request().ReturnNullOnRequestLimit() is { } result)
        {
            _logger.Verbose("Alpaca call completed without retries");
            return result;
        }

        _logger.Verbose("Alpaca call was enqueued");
        _hasCallsRemaining = false;
        return await EnqueueCallAsync(request, logger);
    }

    public async ValueTask DisposeAsync()
    {
        _queuedCalls.Writer.Complete();
        await _queueProcessingTask;
    }

    private async Task<T> EnqueueCallAsync<T>(Func<Task<T>> request, ILogger? logger) where T : class
    {
        var queued = new QueuedAlpacaCall<T>(request, new TaskCompletionSource<T>(), logger);
        _queuedCalls.Writer.TryWrite(queued);
        return await queued.TaskSource.Task;
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            while (await _queuedCalls.Reader.WaitToReadAsync())
            {
                var nextCall = await _queuedCalls.Reader.ReadAsync();
                await RetryCallAsync(nextCall);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "An exception occured while processing queued Alpaca calls");
            throw;
        }
    }

    private async Task RetryCallAsync(QueuedAlpacaCall nextCall)
    {
        if (!_hasCallsRemaining)
        {
            _logger.Debug("No Alpaca calls available - call will be retried after delay");
            await _delay(TimeSpan.FromSeconds(10));
        }

        _logger.Verbose("Retrying Alpaca call");
        if (await nextCall.RetryAsync())
        {
            _logger.Verbose("Alpaca call retry was successful");
            _hasCallsRemaining = true;
            return;
        }

        _logger.Verbose("Alpaca call retry failed");
        _hasCallsRemaining = false;
        _queuedCalls.Writer.TryWrite(nextCall);
    }

    private abstract record QueuedAlpacaCall
    {
        public abstract Task<bool> RetryAsync();
    }

    private sealed record QueuedAlpacaCall<T>(Func<Task<T>> Request, TaskCompletionSource<T> TaskSource,
        ILogger? Logger = null) : QueuedAlpacaCall where T : class
    {
        public override async Task<bool> RetryAsync()
        {
            if (await Request().ReturnNullOnRequestLimit() is not { } result)
            {
                Logger?.Debug("Alpaca call retry failed - it was returned to the queue");
                return false;
            }

            Logger?.Debug("Alpaca call retry succeeded");
            TaskSource.SetResult(result);
            return true;
        }
    }
}
