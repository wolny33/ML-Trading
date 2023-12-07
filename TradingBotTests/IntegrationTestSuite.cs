using Alpaca.Markets;
using Flurl.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Quartz;
using TradingBot;
using TradingBot.Database;
using TradingBot.Services.AlpacaClients;

namespace TradingBotTests;

public class IntegrationTestSuite : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public IntegrationTestSuite()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public IAlpacaAssetsClient AssetsClientSubstitute { get; } = Substitute.For<IAlpacaAssetsClient>();
    public IAlpacaDataClient DataClientSubstitute { get; } = Substitute.For<IAlpacaDataClient>();
    public IAlpacaTradingClient TradingClientSubstitute { get; } = Substitute.For<IAlpacaTradingClient>();

    public IDbContextFactory<AppDbContext> DbContextFactory =>
        Services.GetRequiredService<IDbContextFactory<AppDbContext>>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAlpacaClientFactory>();
            var factory = Substitute.For<IAlpacaClientFactory>();
            SetUpAlpacaSubstitutes(DataClientSubstitute, TradingClientSubstitute, AssetsClientSubstitute);
            factory.CreateTradingClientAsync(Arg.Any<CancellationToken>()).Returns(TradingClientSubstitute);
            factory.CreateMarketDataClientAsync(Arg.Any<CancellationToken>()).Returns(DataClientSubstitute);
            factory.CreateAvailableAssetsClient().Returns(AssetsClientSubstitute);
            services.AddSingleton(factory);

            RemoveDatabaseServices(services);
            services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(_connection));

            // Remove Quartz service to not run jobs in tests
            var quartzService = services.Where(s => s.ImplementationType == typeof(QuartzHostedService)).ToList();
            foreach (var service in quartzService) services.Remove(service);

            ConfigureServices(services);
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

    protected virtual void ConfigureServices(IServiceCollection services)
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
