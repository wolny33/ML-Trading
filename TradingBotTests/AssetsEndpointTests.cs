using FluentAssertions;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using TradingBot.Dto;

namespace TradingBotTests;

[Trait("Category", "Integration")]
public sealed class AssetsEndpointTests : IClassFixture<IntegrationTestSuite>
{
    private readonly IntegrationTestSuite _testSuite;

    public AssetsEndpointTests(IntegrationTestSuite testSuite)
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
    public async Task ShouldReturnMockedReturnsData()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var assets = await client.Request("api", "assets").GetJsonAsync<AssetsResponse>();

        assets.Should().BeEquivalentTo(new
        {
            EquityValue = 11015.98m + 12.04m * 70.66m + 128.97m * 13.06m,
            Cash = new
            {
                AvailableAmount = 11015.98m,
                MainCurrency = "USD"
            },
            Positions = new[]
            {
                new
                {
                    Symbol = "AMZN",
                    Quantity = 12.04m,
                    AvailableQuantity = 12.04m,
                    AverageEntryPrice = 67.54m,
                    MarketValue = 12.04m * 70.66m
                },
                new
                {
                    Symbol = "BBBY",
                    Quantity = 128.97m,
                    AvailableQuantity = 58.97m,
                    AverageEntryPrice = 14.87m,
                    MarketValue = 128.97m * 13.06m
                }
            }
        });
    }
}
