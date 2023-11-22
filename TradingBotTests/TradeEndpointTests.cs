using FluentAssertions;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using TradingBot.Dto;

namespace TradingBotTests;

[Trait("Category", "Integration")]
public sealed class TradeEndpointTests : IClassFixture<IntegrationTestSuite>
{
    private readonly IntegrationTestSuite _testSuite;

    public TradeEndpointTests(IntegrationTestSuite testSuite)
    {
        _testSuite = testSuite;
    }

    [Fact]
    public async Task ShouldReturn401FromAllEndpointsIfUserIsUnauthorized()
    {
        using var client = _testSuite.CreateUnauthenticatedClient().AllowAnyHttpStatus();

        var statusResponse = await client.Request("api", "investment").GetAsync();
        statusResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var updateResponse = await client.Request("api", "investment").PutJsonAsync(new { });
        updateResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ShouldGetInvestmentState()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var investmentState = await client.Request("api", "investment").GetJsonAsync<InvestmentResponse>();

        investmentState.Should().BeEquivalentTo(new
        {
            Enabled = false
        });
    }

    [Fact]
    public async Task ShouldUpdateInvestmentState()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "investment").PutJsonAsync(new
        {
            enable = true
        });
        var investmentState = await response.GetJsonAsync<InvestmentResponse>();

        investmentState.Should().BeEquivalentTo(new
        {
            Enabled = true
        });
    }

    [Fact]
    public async Task ShouldValidateRequest()
    {
        using var client = _testSuite.CreateAuthenticatedClient().AllowAnyHttpStatus();
        var response = await client.Request("api", "investment").PutJsonAsync(new
        {
            disable = false
        });

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}
