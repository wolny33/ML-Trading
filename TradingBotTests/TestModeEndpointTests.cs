using FluentAssertions;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TradingBot.Database.Entities;
using TradingBot.Dto;

namespace TradingBotTests;

[Trait("Category", "Integration")]
public sealed class TestModeEndpointTests : IClassFixture<IntegrationTestSuite>
{
    private readonly IntegrationTestSuite _testSuite;

    public TestModeEndpointTests(IntegrationTestSuite testSuite)
    {
        _testSuite = testSuite;
    }

    [Fact]
    public async Task ShouldReturn401FromAllEndpointsIfUserIsUnauthorized()
    {
        using var client = _testSuite.CreateUnauthenticatedClient().AllowAnyHttpStatus();

        var statusResponse = await client.Request("api", "test-mode").GetAsync();
        statusResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var updateResponse = await client.Request("api", "test-mode").PutJsonAsync(new { });
        updateResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ShouldGetDefaultTestModeState()
    {
        await using (var context = await _testSuite.DbContextFactory.CreateDbContextAsync())
        {
            await context.TestModeConfiguration.ExecuteDeleteAsync();
        }

        using var client = _testSuite.CreateAuthenticatedClient();
        var testModeState = await client.Request("api", "test-mode").GetJsonAsync<TestModeResponse>();

        testModeState.Should().BeEquivalentTo(new
        {
            Enabled = true
        });
    }

    [Fact]
    public async Task ShouldGetTestModeStateFromDb()
    {
        await using (var context = await _testSuite.DbContextFactory.CreateDbContextAsync())
        {
            await context.TestModeConfiguration.ExecuteDeleteAsync();
            context.TestModeConfiguration.Add(new TestModeConfigEntity
            {
                Id = Guid.NewGuid(),
                Enabled = false
            });
            await context.SaveChangesAsync();
        }

        using var client = _testSuite.CreateAuthenticatedClient();
        var testModeState = await client.Request("api", "test-mode").GetJsonAsync<TestModeResponse>();

        testModeState.Should().BeEquivalentTo(new
        {
            Enabled = false
        });
    }

    [Fact]
    public async Task ShouldUpdateTestModeState()
    {
        await using (var context = await _testSuite.DbContextFactory.CreateDbContextAsync())
        {
            await context.TestModeConfiguration.ExecuteDeleteAsync();
            context.TestModeConfiguration.Add(new TestModeConfigEntity
            {
                Id = Guid.NewGuid(),
                Enabled = false
            });
            await context.SaveChangesAsync();
        }

        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "test-mode").PutJsonAsync(new
        {
            enable = true
        });
        var testModeState = await response.GetJsonAsync<TestModeResponse>();

        testModeState.Should().BeEquivalentTo(new
        {
            Enabled = true
        });

        await using (var context = await _testSuite.DbContextFactory.CreateDbContextAsync())
        {
            context.TestModeConfiguration.Should().ContainSingle().Which.Enabled.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ShouldValidateRequest()
    {
        using var client = _testSuite.CreateAuthenticatedClient().AllowAnyHttpStatus();
        var response = await client.Request("api", "test-mode").PutJsonAsync(new
        {
            disable = false
        });

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}
