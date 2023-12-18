using Alpaca.Markets;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Options;
using TradingBot.Configuration;
using Environments = Alpaca.Markets.Environments;

namespace TradingBot.Services.AlpacaClients;

public interface IAlpacaClientFactory
{
    Task<IAlpacaDataClient> CreateMarketDataClientAsync(CancellationToken token = default);
    Task<IAlpacaTradingClient> CreateTradingClientAsync(CancellationToken token = default);
}

public sealed class AlpacaClientFactory : IAlpacaClientFactory
{
    private readonly IOptionsMonitor<AlpacaConfiguration> _config;
    private readonly IFlurlClientFactory _flurlClientFactory;
    private readonly ITestModeConfigService _testModeConfig;

    public AlpacaClientFactory(IOptionsMonitor<AlpacaConfiguration> config, ITestModeConfigService testModeConfig,
        IFlurlClientFactory flurlClientFactory)
    {
        _config = config;
        _testModeConfig = testModeConfig;
        _flurlClientFactory = flurlClientFactory;
    }

    public async Task<IAlpacaDataClient> CreateMarketDataClientAsync(CancellationToken token = default)
    {
        var environment = await GetEnvironmentAsync(token);
        return environment.GetAlpacaDataClient(new SecretKey(_config.CurrentValue.Trading.Key,
            _config.CurrentValue.Trading.Secret));
    }

    public async Task<IAlpacaTradingClient> CreateTradingClientAsync(CancellationToken token = default)
    {
        var environment = await GetEnvironmentAsync(token);
        return environment.GetAlpacaTradingClient(new SecretKey(_config.CurrentValue.Trading.Key,
            _config.CurrentValue.Trading.Secret));
    }

    private async Task<IEnvironment> GetEnvironmentAsync(CancellationToken token = default)
    {
        return (await _testModeConfig.GetConfigurationAsync(token)).Enabled ? Environments.Paper : Environments.Live;
    }
}
