using Alpaca.Markets;
using Flurl.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Quartz;
using TradingBot;
using TradingBot.Database;
using TradingBot.Services;

namespace TradingBotTests;

public class IntegrationTestSuite : WebApplicationFactory<Program>
{
    public const string TestUsername = "test-user";
    public const string TestPassword = "test-password";

    private readonly SqliteConnection _connection;

    public IntegrationTestSuite()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public IAlpacaDataClient DataClientSubstitute { get; } = Substitute.For<IAlpacaDataClient>();
    public IAlpacaTradingClient TradingClientSubstitute { get; } = Substitute.For<IAlpacaTradingClient>();

    public IDbContextFactory<AppDbContext> DbContextFactory =>
        Services.GetRequiredService<IDbContextFactory<AppDbContext>>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedCredentials:DefaultUsername"] = TestUsername,
                ["SeedCredentials:DefaultPassword"] = TestPassword
            });
        }).ConfigureServices(services =>
        {
            services.RemoveAll<IAlpacaClientFactory>();
            var factory = Substitute.For<IAlpacaClientFactory>();
            SetUpAlpacaSubstitutes(DataClientSubstitute, TradingClientSubstitute);
            factory.CreateTradingClientAsync(Arg.Any<CancellationToken>()).Returns(TradingClientSubstitute);
            factory.CreateMarketDataClientAsync(Arg.Any<CancellationToken>()).Returns(DataClientSubstitute);
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

    protected virtual void SetUpAlpacaSubstitutes(IAlpacaDataClient dataClient, IAlpacaTradingClient tradingClient)
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
        return CreateUnauthenticatedClient().WithBasicAuth(TestUsername, TestPassword);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Close();
    }
}
