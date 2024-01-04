using System.Diagnostics.CodeAnalysis;
using Alpaca.Markets;
using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IAssetsDataSource
{
    /// <summary>
    ///     Gets current state of held assets - either from cache or from Alpaca
    /// </summary>
    /// <returns>Held <see cref="Assets" /></returns>
    Task<Assets> GetCurrentAssetsAsync(CancellationToken token = default);

    /// <summary>
    ///     Gets most recent, immediately available assets data
    /// </summary>
    /// <remarks>
    ///     This method behaves in the same way as <see cref="GetCurrentAssetsAsync" />, except in case when Alpaca call
    ///     limit was reached. In this case, the method does not wait for refresh, but instead returns latest data from
    ///     the database, or <c>null</c> if no assets data was saved yet.
    /// </remarks>
    /// <returns>Held <see cref="Assets" /></returns>
    Task<Assets?> GetLatestAssetsAsync(CancellationToken token = default);
}

public sealed class AssetsDataSource : IAssetsDataSource, IAsyncDisposable
{
    private readonly IAssetsStateQuery _assetsStateQuery;
    private readonly IBacktestAssets _backtestAssets;
    private readonly IAlpacaCallQueue _callQueue;
    private readonly ILogger _logger;
    private readonly Lazy<Task<IAlpacaTradingClient>> _tradingClient;
    private readonly ICurrentTradingTask _tradingTask;

    public AssetsDataSource(IAlpacaClientFactory clientFactory, ILogger logger, IAlpacaCallQueue callQueue,
        IAssetsStateQuery assetsStateQuery, IBacktestAssets backtestAssets, ICurrentTradingTask tradingTask)
    {
        _callQueue = callQueue;
        _assetsStateQuery = assetsStateQuery;
        _backtestAssets = backtestAssets;
        _tradingTask = tradingTask;
        _logger = logger.ForContext<AssetsDataSource>();
        _tradingClient = new Lazy<Task<IAlpacaTradingClient>>(() => clientFactory.CreateTradingClientAsync());
    }

    public async Task<Assets> GetCurrentAssetsAsync(CancellationToken token = default)
    {
        if (_tradingTask.CurrentBacktestId is { } backtestId)
        {
            _logger.Verbose("Backtest is active - getting assets for current backtest");
            return _backtestAssets.GetForBacktestWithId(backtestId);
        }

        _logger.Debug("Getting current assets data");
        var (account, positions) = await SendRequestsWithRetriesAsync(token);

        return CreateAssets(account, positions);
    }

    public async Task<Assets?> GetLatestAssetsAsync(CancellationToken token = default)
    {
        _logger.Debug("Getting most recent available assets data");
        if (await SendRequestsWithoutRetriesAsync(token) is { } responses)
        {
            var (account, positions) = responses;
            return CreateAssets(account, positions);
        }

        var lastState = await _assetsStateQuery.GetLatestStateAsync(token);
        return lastState?.Assets;
    }

    public async ValueTask DisposeAsync()
    {
        if (_tradingClient.IsValueCreated) (await _tradingClient.Value).Dispose();
    }

    private static Assets CreateAssets(IAccount account, IEnumerable<IPosition> positions)
    {
        return new Assets
        {
            EquityValue = account.Equity ?? 0m,
            Cash = new Cash
            {
                MainCurrency = account.Currency ?? "Unspecified",
                AvailableAmount = account.TradableCash,
                BuyingPower = account.BuyingPower ?? 0
            },
            Positions = positions.Select(p => new Position
            {
                Symbol = new TradingSymbol(p.Symbol),
                SymbolId = p.AssetId,
                Quantity = p.Quantity,
                AvailableQuantity = p.AvailableQuantity,
                MarketValue = p.MarketValue ?? 0m,
                AverageEntryPrice = p.AverageEntryPrice
            }).ToDictionary(p => p.Symbol)
        };
    }

    private async Task<AlpacaResponses?> SendRequestsWithoutRetriesAsync(CancellationToken token = default)
    {
        _logger.Debug("Sending requests to Alpaca");
        var client = await _tradingClient.Value;
        var account = await client.GetAccountAsync(token).ExecuteWithErrorHandling(_logger).ReturnNullOnRequestLimit();
        var positions = await client.ListPositionsAsync(token).ReturnNullOnRequestLimit(_logger)
            .ExecuteWithErrorHandling(_logger);
        return account is not null && positions is not null ? new AlpacaResponses(account, positions) : null;
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    private async Task<AlpacaResponses> SendRequestsWithRetriesAsync(CancellationToken token = default)
    {
        _logger.Debug("Sending requests to Alpaca");
        var client = await _tradingClient.Value;
        var account = await _callQueue.SendRequestWithRetriesAsync(() => client.GetAccountAsync(token), _logger)
            .ExecuteWithErrorHandling(_logger);
        var positions = await _callQueue.SendRequestWithRetriesAsync(() =>
            client.ListPositionsAsync(token).ExecuteWithErrorHandling(_logger), _logger);
        return new AlpacaResponses(account, positions);
    }

    private sealed record AlpacaResponses(IAccount Account, IReadOnlyList<IPosition> Positions);
}
