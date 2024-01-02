using Serilog;
using TradingBot.Services;

namespace TradingBotTests;

public sealed class CallQueueMock : IAlpacaCallQueue
{
    public Task<T> SendRequestWithRetriesAsync<T>(Func<Task<T>> request, ILogger? logger = null) where T : class
    {
        return request();
    }
}
