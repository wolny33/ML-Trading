using Alpaca.Markets;
using Microsoft.Extensions.Options;
using TradingBot.Configuration;
using Environments = Alpaca.Markets.Environments;

namespace TradingBot.Services;

public interface IAlpacaClientFactory
{
    Task<IAlpacaDataClient> CreateMarketDataClientAsync();
    Task<IAlpacaTradingClient> CreateTradingClientAsync();
}

public sealed class AlpacaClientFactory : IAlpacaClientFactory
{
    private readonly IOptionsMonitor<AlpacaConfiguration> _config;
    private readonly ITestModeConfigService _testModeConfig;

    public AlpacaClientFactory(IOptionsMonitor<AlpacaConfiguration> config, ITestModeConfigService testModeConfig)
    {
        _config = config;
        _testModeConfig = testModeConfig;
    }

    public async Task<IAlpacaDataClient> CreateMarketDataClientAsync()
    {
        var environment = await GetEnvironmentAsync();
        return environment.GetAlpacaDataClient(new SecretKey(_config.CurrentValue.Key,
            _config.CurrentValue.Secret));
    }

    public async Task<IAlpacaTradingClient> CreateTradingClientAsync()
    {
        var environment = await GetEnvironmentAsync();
        return environment.GetAlpacaTradingClient(new SecretKey(_config.CurrentValue.Key,
            _config.CurrentValue.Secret));
    }

    private async Task<IEnvironment> GetEnvironmentAsync()
    {
        return (await _testModeConfig.GetConfigurationAsync()).Enabled ? Environments.Paper : Environments.Live;
    }
}
