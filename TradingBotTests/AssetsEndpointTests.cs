using Alpaca.Markets;
using FluentAssertions;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using TradingBot.Dto;

namespace TradingBotTests;

public sealed class AssetsTestSuite : IntegrationTestSuite
{
    protected override void SetUpAlpacaSubstitutes(IAlpacaDataClient dataClient, IAlpacaTradingClient tradingClient)
    {
        var account = Substitute.For<IAccount>();
        account.Equity.Returns(12345m);
        account.Currency.Returns("EUR");
        account.TradableCash.Returns(1234m);
        tradingClient.GetAccountAsync(Arg.Any<CancellationToken>()).Returns(account);

        var amznPosition = Substitute.For<IPosition>();
        amznPosition.Symbol.Returns("AMZN");
        amznPosition.Quantity.Returns(100m);
        amznPosition.AvailableQuantity.Returns(80m);
        amznPosition.MarketValue.Returns(23456m);
        amznPosition.AverageEntryPrice.Returns(234m);
        var bbbyPosition = Substitute.For<IPosition>();
        bbbyPosition.Symbol.Returns("BBBY");
        bbbyPosition.Quantity.Returns(120m);
        bbbyPosition.AvailableQuantity.Returns(90m);
        bbbyPosition.MarketValue.Returns(1357m);
        bbbyPosition.AverageEntryPrice.Returns(9.81m);
        tradingClient.ListPositionsAsync(Arg.Any<CancellationToken>()).Returns(new[] { bbbyPosition, amznPosition });
    }
}

[Trait("Category", "Integration")]
public sealed class AssetsEndpointTests : IClassFixture<AssetsTestSuite>
{
    private readonly AssetsTestSuite _testSuite;

    public AssetsEndpointTests(AssetsTestSuite testSuite)
    {
        _testSuite = testSuite;
    }

    [Fact]
    public async Task ShouldReturn401FromAllEndpointsIfUserIsUnauthorized()
    {
        using var client = _testSuite.CreateUnauthenticatedClient().AllowAnyHttpStatus();

        var assetsResponse = await client.Request("api", "assets").GetAsync();
        assetsResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ShouldCorrectlyReturnAssetsData()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var assets = await client.Request("api", "assets").GetJsonAsync<AssetsResponse>();

        assets.Should().BeEquivalentTo(new
        {
            EquityValue = 12345m,
            Cash = new
            {
                AvailableAmount = 1234m,
                MainCurrency = "EUR"
            },
            Positions = new[]
            {
                new
                {
                    Symbol = "AMZN",
                    Quantity = 100m,
                    AvailableQuantity = 80m,
                    AverageEntryPrice = 234m,
                    MarketValue = 23456m
                },
                new
                {
                    Symbol = "BBBY",
                    Quantity = 120m,
                    AvailableQuantity = 90m,
                    AverageEntryPrice = 9.81m,
                    MarketValue = 1357m
                }
            }
        });
    }
}
