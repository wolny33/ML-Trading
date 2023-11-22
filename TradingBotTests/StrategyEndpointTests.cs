﻿using FluentAssertions;
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
        var strategySettings = await client.Request("api", "strategy").GetJsonAsync<StrategySettingsResponse>();

        strategySettings.Should().BeEquivalentTo(new
        {
            ImportantProperty = "value"
        });
    }

    [Fact]
    public async Task ShouldUpdateStrategySettings()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "strategy").PutJsonAsync(new
        {
            importantProperty = "new value"
        });
        var strategySettings = await response.GetJsonAsync<StrategySettingsResponse>();

        strategySettings.Should().BeEquivalentTo(new
        {
            ImportantProperty = "new value"
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