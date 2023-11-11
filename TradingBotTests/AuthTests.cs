using FluentAssertions;
using Flurl.Http;
using Microsoft.AspNetCore.Http;

namespace TradingBotTests;

[Trait("Category", "Integration")]
public sealed class AuthTests : IClassFixture<IntegrationTestSuite>
{
    private readonly IntegrationTestSuite _testSuite;

    public AuthTests(IntegrationTestSuite testSuite)
    {
        _testSuite = testSuite;
    }

    [Fact]
    public async Task ShouldReturn401UnauthorizedIfNoCredentialsAreGiven()
    {
        using var client = _testSuite.CreateUnauthenticatedClient();
        var response = await client.AllowAnyHttpStatus().Request("api", "test-mode").GetAsync();
        response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
