using Alpaca.Markets;
using Microsoft.Extensions.Options;
using TradingBot.Configuration;
using Environments = Alpaca.Markets.Environments;

namespace TradingBot.Services;

public interface IAlpacaClientFactory
{
    IAlpacaDataClient CreateMarketDataClient();
    IAlpacaTradingClient CreateTradingClient();
}

public sealed class AlpacaClientFactory : IAlpacaClientFactory
{
    private readonly IOptionsMonitor<AlpacaConfiguration> _config;

    public AlpacaClientFactory(IOptionsMonitor<AlpacaConfiguration> config)
    {
        _config = config;
    }

    public IAlpacaDataClient CreateMarketDataClient()
    {
        return Environments.Paper.GetAlpacaDataClient(new SecretKey(_config.CurrentValue.Key,
            _config.CurrentValue.Secret));
    }

    public IAlpacaTradingClient CreateTradingClient()
    {
        return Environments.Paper.GetAlpacaTradingClient(new SecretKey(_config.CurrentValue.Key,
            _config.CurrentValue.Secret));
    }
}
