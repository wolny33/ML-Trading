using Alpaca.Markets;
using Flurl.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using TradingBot;
using TradingBot.Database;
using TradingBot.Services.Alpaca;

namespace TradingBotTests;

public class IntegrationTestSuite : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;
    private readonly IAlpacaAssetsClient _assetsClientSubstitute = Substitute.For<IAlpacaAssetsClient>();
    private readonly IAlpacaDataClient _dataClientSubstitute = Substitute.For<IAlpacaDataClient>();
    private readonly IAlpacaTradingClient _tradingClientSubstitute = Substitute.For<IAlpacaTradingClient>();

    public IntegrationTestSuite()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public IDbContextFactory<AppDbContext> DbContextFactory =>
        Services.GetRequiredService<IDbContextFactory<AppDbContext>>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAlpacaClientFactory>();
            var factory = Substitute.For<IAlpacaClientFactory>();
            SetUpAlpacaSubstitutes(_dataClientSubstitute, _tradingClientSubstitute, _assetsClientSubstitute);
            factory.CreateTradingClientAsync(Arg.Any<CancellationToken>()).Returns(_tradingClientSubstitute);
            factory.CreateMarketDataClientAsync(Arg.Any<CancellationToken>()).Returns(_dataClientSubstitute);
            factory.CreateAvailableAssetsClient().Returns(_assetsClientSubstitute);
            services.AddSingleton(factory);

            RemoveDatabaseServices(services);
            services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    private static void RemoveDatabaseServices(IServiceCollection services)
    {
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.RemoveAll<DbContextOptions>();
        services.RemoveAll<IDbContextFactory<AppDbContext>>();
        services.RemoveAll<AppDbContext>();
    }

    protected virtual void SetUpAlpacaSubstitutes(IAlpacaDataClient dataClient, IAlpacaTradingClient tradingClient,
        IAlpacaAssetsClient assetsClient)
    {
    }

    public FlurlClient CreateUnauthenticatedClient()
    {
        return new FlurlClient(CreateClient());
    }

    public FlurlClient CreateAuthenticatedClient()
    {
        return CreateUnauthenticatedClient().WithBasicAuth("admin", "password");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Close();
    }
}
