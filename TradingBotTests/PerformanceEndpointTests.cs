using FluentAssertions;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;

namespace TradingBotTests;

[Trait("Category", "Integration")]
public sealed class PerformanceEndpointTests : IClassFixture<IntegrationTestSuite>
{
    private readonly IntegrationTestSuite _testSuite;

    public PerformanceEndpointTests(IntegrationTestSuite testSuite)
    {
        _testSuite = testSuite;
    }

    [Fact]
    public async Task ShouldReturn401FromAllEndpointsIfUserIsUnauthorized()
    {
        using var client = _testSuite.CreateUnauthenticatedClient().AllowAnyHttpStatus();

        var performanceResponse = await client.Request("api", "performance").GetAsync();
        performanceResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var actionsResponse = await client.Request("api", "performance", "trade-actions").GetAsync();
        actionsResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var detailsResponse = await client.Request("api", "performance", "trade-actions", Guid.NewGuid()).GetAsync();
        detailsResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ShouldReturnMockedReturnsData()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var returns = await client.Request("api", "performance").GetJsonAsync<IReadOnlyList<ReturnResponse>>();

        returns.Should().HaveCount(10).And.BeInAscendingOrder(r => r.Time);
    }

    [Fact]
    public async Task ShouldReturnReturnsDataWhenUsingQueryParameters()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.AllowAnyHttpStatus().Request("api", "performance")
            .SetQueryParams(new
            {
                start = "2023-11-22T11:19:00",
                end = "2023-11-24T10:10:00"
            }).GetAsync();
        var returns = await response.GetJsonAsync<IReadOnlyList<ReturnResponse>>();

        returns.Should().HaveCount(2).And.BeInAscendingOrder(r => r.Time);
    }

    [Fact]
    public async Task ShouldValidateQueryParametersWhenRequestingReturnsData()
    {
        using var client = _testSuite.CreateAuthenticatedClient().AllowAnyHttpStatus();
        var response = await client.Request("api", "performance")
            .SetQueryParams(new
            {
                start = new DateTimeOffset(2023, 11, 22, 11, 19, 0, TimeSpan.Zero),
                end = new DateTimeOffset(2023, 11, 20, 10, 10, 0, TimeSpan.Zero)
            }).GetAsync();

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var errors = await response.GetJsonAsync<ValidationProblemDetails>();
        errors.Errors.Should().ContainKey(nameof(ReturnsRequest.Start)).WhoseValue.Should()
            .ContainMatch("*must be earlier than*");
    }

    [Fact]
    public async Task ShouldReturnMockedTradeActions()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var returns = await client.Request("api", "performance", "trade-actions")
            .GetJsonAsync<IReadOnlyList<TradingActionResponse>>();

        returns.Should().HaveCount(10).And.BeInAscendingOrder(a => a.CreatedAt);
    }

    [Fact]
    public async Task ShouldReturnTradeActionsWhenUsingQueryParameters()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var returns = await client.Request("api", "performance", "trade-actions")
            .SetQueryParams(new
            {
                start = "2023-11-22T11:19:00",
                end = "2023-11-24T10:10:00"
            }).GetJsonAsync<IReadOnlyList<TradingActionResponse>>();

        returns.Should().HaveCount(2).And.BeInAscendingOrder(a => a.CreatedAt);
    }

    [Fact]
    public async Task ShouldValidateQueryParametersWhenRequestingTradeActions()
    {
        using var client = _testSuite.CreateAuthenticatedClient().AllowAnyHttpStatus();
        var response = await client.Request("api", "performance", "trade-actions")
            .SetQueryParams(new
            {
                start = new DateTimeOffset(2023, 11, 22, 11, 19, 0, TimeSpan.Zero),
                end = new DateTimeOffset(2023, 11, 20, 10, 10, 0, TimeSpan.Zero)
            }).GetAsync();

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var errors = await response.GetJsonAsync<ValidationProblemDetails>();
        errors.Errors.Should().ContainKey(nameof(TradingActionRequest.Start)).WhoseValue.Should()
            .ContainMatch("*must be earlier than*");
    }

    [Fact]
    public async Task ShouldReturnMockedTradeActionDetails()
    {
        using var client = _testSuite.CreateAuthenticatedClient();

        var id = Guid.NewGuid();
        var returns = await client.Request("api", "performance", "trade-actions", id)
            .GetJsonAsync<TradingActionDetailsResponse>();

        returns.Should().BeEquivalentTo(new
        {
            Id = id
        });
    }
}
