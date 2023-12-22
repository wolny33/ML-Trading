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

    Task<Assets> GetMockedAssetsAsync(CancellationToken token = default);
}

public sealed class AssetsDataSource : IAssetsDataSource
{
    private readonly IAssetsStateQuery _assetsStateQuery;
    private readonly IAlpacaCallQueue _callQueue;
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly ILogger _logger;

    public AssetsDataSource(IAlpacaClientFactory clientFactory, ILogger logger, IAlpacaCallQueue callQueue,
        IAssetsStateQuery assetsStateQuery)
    {
        _clientFactory = clientFactory;
        _callQueue = callQueue;
        _assetsStateQuery = assetsStateQuery;
        _logger = logger.ForContext<AssetsDataSource>();
    }

    public async Task<Assets> GetCurrentAssetsAsync(CancellationToken token = default)
    {
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

    public Task<Assets> GetMockedAssetsAsync(CancellationToken token = default)
    {
        return Task.FromResult(new Assets
        {
            EquityValue = 11015.98m + 12.04m * 70.66m + 128.97m * 13.06m,
            Cash = new Cash
            {
                AvailableAmount = 11015.98m,
                MainCurrency = "USD",
                BuyingPower = 9024.56m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("AMZN")] = new()
                {
                    SymbolId = Guid.NewGuid(),
                    Symbol = new TradingSymbol("AMZN"),
                    Quantity = 12.04m,
                    AvailableQuantity = 12.04m,
                    AverageEntryPrice = 67.54m,
                    MarketValue = 12.04m * 70.66m
                },
                [new TradingSymbol("BBBY")] = new()
                {
                    SymbolId = Guid.NewGuid(),
                    Symbol = new TradingSymbol("BBBY"),
                    Quantity = 128.97m,
                    AvailableQuantity = 58.97m,
                    AverageEntryPrice = 14.87m,
                    MarketValue = 128.97m * 13.06m
                }
            }
        });
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
        using var client = await _clientFactory.CreateTradingClientAsync(token);
        var account = await client.GetAccountAsync(token).ExecuteWithErrorHandling(_logger).ReturnNullOnRequestLimit();
        var positions = await client.ListPositionsAsync(token).ExecuteWithErrorHandling(_logger)
            .ReturnNullOnRequestLimit();
        return account is not null && positions is not null ? new AlpacaResponses(account, positions) : null;
    }

    private async Task<AlpacaResponses> SendRequestsWithRetriesAsync(CancellationToken token = default)
    {
        _logger.Debug("Sending requests to Alpaca");
        using var client = await _clientFactory.CreateTradingClientAsync(token);
        var account = await _callQueue.SendRequestWithRetriesAsync(() =>
            client.GetAccountAsync(token).ExecuteWithErrorHandling(_logger), _logger);
        var positions = await _callQueue.SendRequestWithRetriesAsync(() =>
            client.ListPositionsAsync(token).ExecuteWithErrorHandling(_logger), _logger);
        return new AlpacaResponses(account, positions);
    }

    private sealed record AlpacaResponses(IAccount Account, IReadOnlyList<IPosition> Positions);
}
