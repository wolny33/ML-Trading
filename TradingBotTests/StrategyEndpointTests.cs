using FluentAssertions;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using TradingBot.Dto;

namespace TradingBotTests;

[Trait("Category", "Integration")]
public sealed class StrategyEndpointTests : IClassFixture<IntegrationTestSuite>
{
    private readonly IntegrationTestSuite _testSuite;

    public StrategyEndpointTests(IntegrationTestSuite testSuite)
    {
        _testSuite = testSuite;
    }

    [Fact]
    public async Task ShouldReturn401FromAllEndpointsIfUserIsUnauthorized()
    {
        using var client = _testSuite.CreateUnauthenticatedClient().AllowAnyHttpStatus();

        var statusResponse = await client.Request("api", "strategy").GetAsync();
        statusResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var updateResponse = await client.Request("api", "strategy").PutJsonAsync(new { });
        updateResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ShouldGetStrategySettings()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var strategySettings = await client.Request("api", "strategy").GetJsonAsync<StrategyParametersResponse>();

        strategySettings.Should().BeEquivalentTo(new
        {
            MaxStocksBuyCount = 10,
            MinDaysDecreasing = 5,
            MinDaysIncreasing = 5,
            TopGrowingSymbolsBuyRatio = 0.4m
        });
    }

    [Fact]
    public async Task ShouldUpdateStrategySettings()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "strategy").PutJsonAsync(new
        {
            MaxStocksBuyCount = 12,
            MinDaysDecreasing = 3,
            MinDaysIncreasing = 3,
            TopGrowingSymbolsBuyRatio = 0.5m
        });
        var strategySettings = await response.GetJsonAsync<StrategyParametersResponse>();

        strategySettings.Should().BeEquivalentTo(new
        {
            MaxStocksBuyCount = 12,
            MinDaysDecreasing = 3,
            MinDaysIncreasing = 3,
            TopGrowingSymbolsBuyRatio = 0.5m
        });
    }

    [Fact]
    public async Task ShouldValidateRequest()
    {
        using var client = _testSuite.CreateAuthenticatedClient().AllowAnyHttpStatus();
        var response = await client.Request("api", "strategy").PutJsonAsync(new
        {
            disable = false
        });

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}
