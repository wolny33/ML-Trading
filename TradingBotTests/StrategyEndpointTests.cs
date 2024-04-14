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

        var selectionResponse = await client.Request("api", "strategy", "selection").GetAsync();
        selectionResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var updateSelectionResponse = await client.Request("api", "strategy", "selection").PutJsonAsync(new { });
        updateSelectionResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var namesResponse = await client.Request("api", "strategy", "selection", "names").GetAsync();
        namesResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ShouldGetStrategySettings()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var strategySettings = await client.Request("api", "strategy").GetJsonAsync<StrategyParametersResponse>();

        strategySettings.Should().BeEquivalentTo(new
        {
            LimitPriceDamping = 0.5m,
            Basic = new
            {
                MaxStocksBuyCount = 10,
                MinDaysDecreasing = 5,
                MinDaysIncreasing = 5,
                TopGrowingSymbolsBuyRatio = 0.4
            },
            BuyLosers = new
            {
                AnalysisLengthInDays = 30,
                EvaluationFrequencyInDays = 30
            },
            BuyWinners = new
            {
                AnalysisLengthInDays = 360,
                EvaluationFrequencyInDays = 30,
                SimultaneousEvaluations = 3,
                BuyWaitTimeInDays = 7
            },
            Pca = new
            {
                AnalysisLengthInDays = 90,
                DecompositionExpirationInDays = 7,
                UndervaluedThreshold = 1,
                VarianceFraction = 0.9,
                IgnoredThreshold = 0.25,
                DiverseThreshold = 0.5
            }
        }, options => options.Excluding(r => r.Pca.VarianceFraction));

        strategySettings.Pca.VarianceFraction.Should().BeApproximately(0.9, 1e-5);
    }

    [Fact]
    public async Task ShouldUpdateStrategySettings()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "strategy").PutJsonAsync(new
        {
            LimitPriceDamping = 0.5m,
            Basic = new
            {
                MaxStocksBuyCount = 12,
                MinDaysDecreasing = 3,
                MinDaysIncreasing = 3,
                TopGrowingSymbolsBuyRatio = 0.5
            },
            BuyLosers = new
            {
                AnalysisLengthInDays = 30,
                EvaluationFrequencyInDays = 30
            },
            BuyWinners = new
            {
                AnalysisLengthInDays = 360,
                EvaluationFrequencyInDays = 30,
                SimultaneousEvaluations = 3,
                BuyWaitTimeInDays = 7
            },
            Pca = new
            {
                AnalysisLengthInDays = 90,
                DecompositionExpirationInDays = 7,
                UndervaluedThreshold = 1,
                VarianceFraction = 0.9,
                IgnoredThreshold = 0.25,
                DiverseThreshold = 0.5
            }
        });
        var strategySettings = await response.GetJsonAsync<StrategyParametersResponse>();

        await client.Request("api", "strategy").PutJsonAsync(new
        {
            LimitPriceDamping = 0.5m,
            Basic = new
            {
                MaxStocksBuyCount = 10,
                MinDaysDecreasing = 5,
                MinDaysIncreasing = 5,
                TopGrowingSymbolsBuyRatio = 0.4
            },
            BuyLosers = new
            {
                AnalysisLengthInDays = 30,
                EvaluationFrequencyInDays = 30
            },
            BuyWinners = new
            {
                AnalysisLengthInDays = 360,
                EvaluationFrequencyInDays = 30,
                SimultaneousEvaluations = 3,
                BuyWaitTimeInDays = 7
            },
            Pca = new
            {
                AnalysisLengthInDays = 90,
                DecompositionExpirationInDays = 7,
                UndervaluedThreshold = 1,
                VarianceFraction = 0.9,
                IgnoredThreshold = 0.25,
                DiverseThreshold = 0.5
            }
        });

        strategySettings.Should().BeEquivalentTo(new
        {
            LimitPriceDamping = 0.5m,
            Basic = new
            {
                MaxStocksBuyCount = 12,
                MinDaysDecreasing = 3,
                MinDaysIncreasing = 3,
                TopGrowingSymbolsBuyRatio = 0.5
            },
            BuyLosers = new
            {
                AnalysisLengthInDays = 30,
                EvaluationFrequencyInDays = 30
            },
            BuyWinners = new
            {
                AnalysisLengthInDays = 360,
                EvaluationFrequencyInDays = 30,
                SimultaneousEvaluations = 3,
                BuyWaitTimeInDays = 7
            },
            Pca = new
            {
                AnalysisLengthInDays = 90,
                DecompositionExpirationInDays = 7,
                UndervaluedThreshold = 1,
                VarianceFraction = 0.9,
                IgnoredThreshold = 0.25,
                DiverseThreshold = 0.5
            }
        }, options => options.Excluding(r => r.Pca.VarianceFraction));

        strategySettings.Pca.VarianceFraction.Should().BeApproximately(0.9, 1e-5);
    }

    [Fact]
    public async Task ShouldValidateStrategyParametersRequest()
    {
        using var client = _testSuite.CreateAuthenticatedClient().AllowAnyHttpStatus();
        var response = await client.Request("api", "strategy").PutJsonAsync(new { disable = false });

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ShouldGetSelectedStrategy()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var selectedStrategy =
            await client.Request("api", "strategy", "selection").GetJsonAsync<StrategySelectionResponse>();

        selectedStrategy.Should().BeEquivalentTo(new { Name = "Basic strategy" });
    }

    [Fact]
    public async Task ShouldUpdateStrategySelection()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "strategy", "selection").PutJsonAsync(new
        {
            name = "Greedy optimal strategy"
        });
        var strategySelection = await response.GetJsonAsync<StrategySelectionResponse>();

        await client.Request("api", "strategy", "selection").PutJsonAsync(new { name = "Basic strategy" });

        strategySelection.Should().BeEquivalentTo(new { Name = "Greedy optimal strategy" });
    }

    [Fact]
    public async Task ShouldValidateStrategySelectionRequest()
    {
        using var client = _testSuite.CreateAuthenticatedClient().AllowAnyHttpStatus();
        var response = await client.Request("api", "strategy", "selection").PutJsonAsync(new { name = "unknown name" });

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ShouldGetKnownStrategyNames()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var strategyNames = await client.Request("api", "strategy", "selection", "names")
            .GetJsonAsync<StrategyNamesResponse>();

        strategyNames.Names.Should().Contain("Basic strategy").And.Contain("Greedy optimal strategy");
    }
}
