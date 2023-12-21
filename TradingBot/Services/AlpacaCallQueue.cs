using System.Threading.Channels;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IAlpacaCallQueue
{
    Task<T> SendRequestWithRetriesAsync<T>(Func<Task<T>> request) where T : class;
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
        _logger = logger;
        _queueProcessingTask = ProcessQueueAsync();
    }

    public async Task<T> SendRequestWithRetriesAsync<T>(Func<Task<T>> request) where T : class
    {
        if (_hasCallsRemaining && await request().ReturnNullOnRequestLimit() is { } result) return result;

        _hasCallsRemaining = false;
        return await EnqueueCallAsync(request);
    }

    public async ValueTask DisposeAsync()
    {
        _queuedCalls.Writer.Complete();
        await _queueProcessingTask;
    }

    private async Task<T> EnqueueCallAsync<T>(Func<Task<T>> request) where T : class
    {
        var queued = new QueuedAlpacaCall<T>(request, new TaskCompletionSource<T>());
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
        if (!_hasCallsRemaining) await _delay(TimeSpan.FromSeconds(10));

        if (await nextCall.RetryAsync())
        {
            _hasCallsRemaining = true;
            return;
        }

        _hasCallsRemaining = false;
        _queuedCalls.Writer.TryWrite(nextCall);
    }

    private abstract record QueuedAlpacaCall
    {
        public abstract Task<bool> RetryAsync();
    }

    private sealed record QueuedAlpacaCall<T>
        (Func<Task<T>> Request, TaskCompletionSource<T> TaskSource) : QueuedAlpacaCall where T : class
    {
        public override async Task<bool> RetryAsync()
        {
            if (await Request().ReturnNullOnRequestLimit() is not { } result) return false;
            TaskSource.SetResult(result);
            return true;
        }
    }
}
