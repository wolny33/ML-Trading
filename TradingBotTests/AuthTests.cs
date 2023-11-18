using FluentAssertions;
using Flurl.Http;
using Microsoft.AspNetCore.Http;

namespace TradingBotTests;

[Trait("Category", "Integration")]
public sealed class AuthTests : IAsyncDisposable
{
    private readonly IntegrationTestSuite _testSuite = new();

    public ValueTask DisposeAsync()
    {
        return _testSuite.DisposeAsync();
    }

    [Fact]
    public async Task ShouldReturnOkIfCredentialsAreCorrect()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.AllowAnyHttpStatus().Request("api", "test-mode").GetAsync();
        response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ShouldReturn401UnauthorizedIfNoCredentialsAreGiven()
    {
        using var client = _testSuite.CreateUnauthenticatedClient();
        var response = await client.AllowAnyHttpStatus().Request("api", "test-mode").GetAsync();
        response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ShouldChangePassword()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var firstResponse = await client.Request("api", "settings", "auth").AllowAnyHttpStatus().PutJsonAsync(new
        {
            newPassword = "new-password"
        });
        firstResponse.StatusCode.Should().Be(StatusCodes.Status204NoContent);

        var secondResponse = await client.AllowAnyHttpStatus().Request("api", "test-mode").GetAsync();
        secondResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        using var newClient = _testSuite.CreateUnauthenticatedClient().WithBasicAuth("admin", "new-password");
        var thirdResponse = await newClient.AllowAnyHttpStatus().Request("api", "test-mode").GetAsync();
        thirdResponse.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
