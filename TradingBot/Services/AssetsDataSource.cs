using System.Net.Sockets;
using Alpaca.Markets;
using TradingBot.Exceptions;
using TradingBot.Models;
using TradingBot.Services.AlpacaClients;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IAssetsDataSource
{
    public Task<Assets> GetAssetsAsync(CancellationToken token = default);

    public Task<Assets> GetMockedAssetsAsync(CancellationToken token = default);
}

public sealed class AssetsDataSource : IAssetsDataSource
{
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly ILogger _logger;

    public AssetsDataSource(IAlpacaClientFactory clientFactory, ILogger logger)
    {
        _clientFactory = clientFactory;
        _logger = logger.ForContext<AssetsDataSource>();
    }

    public async Task<Assets> GetAssetsAsync(CancellationToken token = default)
    {
        _logger.Debug("Getting assets");
        using var client = await _clientFactory.CreateTradingClientAsync(token);
        var (account, positions) = await SendRequestsAsync(client, token);

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

    private async Task<AlpacaResponses> SendRequestsAsync(IAlpacaTradingClient client,
        CancellationToken token = default)
    {
        _logger.Debug("Sending requests to Alpaca");

        try
        {
            var account = await client.GetAccountAsync(token);
            var positions = await client.ListPositionsAsync(token);
            return new AlpacaResponses(account, positions);
        }
        catch (RestClientErrorException e) when (e.HttpStatusCode is { } statusCode)
        {
            _logger.Error(e, "Alpaca responded with {StatusCode}", statusCode);
            throw new UnsuccessfulAlpacaResponseException(statusCode, e.ErrorCode, e.Message);
        }
        catch (Exception e) when (e is RestClientErrorException or HttpRequestException or SocketException
                                      or TaskCanceledException)
        {
            _logger.Error(e, "Alpaca request failed");
            throw new AlpacaCallFailedException(e);
        }
    }

    private sealed record AlpacaResponses(IAccount Account, IReadOnlyList<IPosition> Positions);
}
