using System.Net;
using System.Threading.Channels;
using Alpaca.Markets;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IAlpacaCallQueue
{
    Task<T> SendRequestWithRetriesAsync<T>(Func<Task<T>> request);
}

public sealed class AlpacaCallQueue : IAlpacaCallQueue, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly Channel<QueuedAlpacaCall> _queuedCalls = Channel.CreateUnbounded<QueuedAlpacaCall>();
    private readonly Task _queueProcessingTask;
    private bool _isOutOfRequests;

    public AlpacaCallQueue(ILogger logger)
    {
        _logger = logger;
        _queueProcessingTask = ProcessQueueAsync();
    }

    public async Task<T> SendRequestWithRetriesAsync<T>(Func<Task<T>> request)
    {
        if (_isOutOfRequests) return await QueueCallAsync(request);

        try
        {
            return await request();
        }
        catch (RestClientErrorException e) when (e.HttpStatusCode == HttpStatusCode.TooManyRequests)
        {
            _isOutOfRequests = true;
            return await QueueCallAsync(request);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _queuedCalls.Writer.Complete();
        await _queueProcessingTask;
    }

    private async Task<T> QueueCallAsync<T>(Func<Task<T>> request)
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
        if (_isOutOfRequests) await Task.Delay(TimeSpan.FromSeconds(10));

        if (await nextCall.RetryAsync())
        {
            _isOutOfRequests = false;
            return;
        }

        _isOutOfRequests = true;
        _queuedCalls.Writer.TryWrite(nextCall);
    }

    private abstract record QueuedAlpacaCall
    {
        public abstract Task<bool> RetryAsync();
    }

    private sealed record QueuedAlpacaCall<T>
        (Func<Task<T>> Request, TaskCompletionSource<T> TaskSource) : QueuedAlpacaCall
    {
        public override async Task<bool> RetryAsync()
        {
            try
            {
                var response = await Request();
                TaskSource.SetResult(response);
                return true;
            }
            catch (RestClientErrorException e) when (e.HttpStatusCode == HttpStatusCode.TooManyRequests)
            {
                return false;
            }
        }
    }
}
