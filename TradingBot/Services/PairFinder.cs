using System.Diagnostics.CodeAnalysis;

namespace TradingBot.Services;

public interface IPairFinder
{
    void StartNewPairGroupCreation();
    bool IsPairCreationInProgress();
}

// TODO: How should this work in backtests???
public sealed class PairFinder : IPairFinder, IAsyncDisposable
{
    private readonly IPairGroupCommand _pairGroupCommand;
    private Task? _pairCreationTask;
    private CancellationTokenSource _tokenSource = new();

    public PairFinder(IPairGroupCommand pairGroupCommand)
    {
        _pairGroupCommand = pairGroupCommand;
    }

    public async ValueTask DisposeAsync()
    {
        if (!IsPairCreationInProgress())
        {
            _tokenSource.Dispose();
            return;
        }

        _tokenSource.Cancel();
        await _pairCreationTask;
        _tokenSource.Dispose();
    }

    public void StartNewPairGroupCreation()
    {
        if (IsPairCreationInProgress())
        {
            return;
        }

        _tokenSource = new();
        _pairCreationTask ??= CreatePairsAsync(_tokenSource.Token);
    }

    [MemberNotNullWhen(true, nameof(_pairCreationTask))]
    public bool IsPairCreationInProgress()
    {
        return _pairCreationTask?.IsCompleted == false;
    }

    private async Task CreatePairsAsync(CancellationToken token)
    {
        await Task.Yield();

        throw new NotImplementedException();
    }
}
