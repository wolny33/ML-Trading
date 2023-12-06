using TradingBot.Models;

namespace TradingBot.Services;

public interface ITradingTaskCommand
{
    Task<Guid> CreateNewAsync(DateTimeOffset start, CancellationToken token = default);
    Task SetStateAndEndAsync(Guid taskId, TradingTaskCompletionDetails details, CancellationToken token = default);
}

public sealed class TradingTaskCommand : ITradingTaskCommand
{
    public Task<Guid> CreateNewAsync(DateTimeOffset start, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task SetStateAndEndAsync(Guid taskId, TradingTaskCompletionDetails details,
        CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}

public sealed record TradingTaskCompletionDetails(DateTimeOffset End, TradingTaskState State, string StateDescription);
