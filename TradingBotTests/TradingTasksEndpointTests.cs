using Alpaca.Markets;
using FluentAssertions;
using Flurl.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using TradingBot.Database.Entities;
using TradingBot.Dto;
using TradingBot.Models;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBotTests;

public sealed class TradingTasksTestSuite : IntegrationTestSuite, IAsyncLifetime
{
    public static readonly DateTimeOffset Now = new(2023, 12, 7, 15, 58, 0, TimeSpan.Zero);

    public TradingTasksTestSuite()
    {
        var id = Guid.NewGuid();
        Tasks = new[]
        {
            new TradingTaskEntity
            {
                Id = id,
                StartTimestamp = (Now - TimeSpan.FromMinutes(22)).ToUnixTimeMilliseconds(),
                EndTimestamp = (Now - TimeSpan.FromMinutes(21)).ToUnixTimeMilliseconds(),
                State = TradingTaskState.ConfigDisabled,
                StateDetails = "Automatic investing is disabled in configuration"
            },
            new TradingTaskEntity
            {
                Id = Guid.NewGuid(),
                StartTimestamp = (Now - TimeSpan.FromMinutes(20)).ToUnixTimeMilliseconds(),
                EndTimestamp = (Now - TimeSpan.FromMinutes(18)).ToUnixTimeMilliseconds(),
                State = TradingTaskState.Success,
                StateDetails = "Finished successfully",
                TradingActions = new[]
                {
                    new TradingActionEntity
                    {
                        Id = Guid.NewGuid(),
                        AlpacaId = Guid.NewGuid(),
                        CreationTimestamp = (Now - TimeSpan.FromMinutes(19)).ToUnixTimeMilliseconds(),
                        Symbol = "AMZN",
                        OrderType = OrderType.LimitBuy,
                        Quantity = 12.5,
                        Price = 105.5,
                        InForce = TimeInForce.Day,
                        Status = OrderStatus.PartiallyFilled,
                        ExecutionTimestamp = null,
                        AverageFillPrice = 105.34,
                        ErrorCode = null,
                        ErrorMessage = null,
                        TradingTaskId = id
                    }
                }
            },
            new TradingTaskEntity
            {
                Id = Guid.NewGuid(),
                StartTimestamp = (Now - TimeSpan.FromMinutes(18)).ToUnixTimeMilliseconds(),
                EndTimestamp = (Now - TimeSpan.FromMinutes(17)).ToUnixTimeMilliseconds(),
                State = TradingTaskState.Error,
                StateDetails =
                    "Trading task failed with error code unsuccessful-alpaca-response: Alpaca API responded with status code 401: invalid credentials"
            },
            new TradingTaskEntity
            {
                Id = Guid.NewGuid(),
                StartTimestamp = (Now - TimeSpan.FromMinutes(10)).ToUnixTimeMilliseconds(),
                EndTimestamp = null,
                State = TradingTaskState.Running,
                StateDetails = "Trading task is running"
            }
        };
    }

    public IReadOnlyList<TradingTaskEntity> Tasks { get; }

    public Task InitializeAsync()
    {
        return ResetAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeAsync().AsTask();
    }

    protected override void SetUpAlpacaSubstitutes(IAlpacaDataClient dataClient, IAlpacaTradingClient tradingClient)
    {
        var order = Substitute.For<IOrder>();
        order.OrderStatus.Returns(OrderStatus.Filled);
        order.AverageFillPrice.Returns(105.32m);
        order.FilledAtUtc.Returns((Now - TimeSpan.FromMinutes(8)).DateTime);
        tradingClient.GetOrderAsync(Tasks[1].TradingActions[0].AlpacaId!.Value, Arg.Any<CancellationToken>())
            .Returns(order);
    }

    public async Task ResetAsync()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        await context.TradingActions.ExecuteDeleteAsync();
        await context.TradingTasks.ExecuteDeleteAsync();
        context.TradingTasks.AddRange(Tasks);
        await context.SaveChangesAsync();

        TradingClientSubstitute.ClearReceivedCalls();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<ISystemClock>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(Now);
        services.AddSingleton(clock);
    }
}

[Trait("Category", "Integration")]
public sealed class TradingTasksEndpointTests : IClassFixture<TradingTasksTestSuite>
{
    private readonly TradingTasksTestSuite _testSuite;

    public TradingTasksEndpointTests(TradingTasksTestSuite testSuite)
    {
        _testSuite = testSuite;
    }

    [Fact]
    public async Task ShouldReturn401FromAllEndpointsIfUserIsUnauthorized()
    {
        using var client = _testSuite.CreateUnauthenticatedClient().AllowAnyHttpStatus();

        var performanceResponse = await client.Request("api", "trading-tasks").GetAsync();
        performanceResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var actionsResponse = await client.Request("api", "trading-tasks", Guid.NewGuid()).GetAsync();
        actionsResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var detailsResponse =
            await client.Request("api", "trading-tasks", Guid.NewGuid(), "trading-actions").GetAsync();
        detailsResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ShouldGetAllTradingTasks()
    {
        await _testSuite.ResetAsync();

        using var client = _testSuite.CreateAuthenticatedClient();
        var tasks = await client.Request("api", "trading-tasks").GetJsonAsync<IReadOnlyList<TradingTaskResponse>>();
        tasks.Should().BeEquivalentTo(new[]
        {
            new TradingTaskResponse
            {
                Id = _testSuite.Tasks[0].Id,
                StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[0].StartTimestamp),
                FinishedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[0].EndTimestamp!.Value),
                State = _testSuite.Tasks[0].State.ToString(),
                StateDetails = _testSuite.Tasks[0].StateDetails
            },
            new TradingTaskResponse
            {
                Id = _testSuite.Tasks[1].Id,
                StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[1].StartTimestamp),
                FinishedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[1].EndTimestamp!.Value),
                State = _testSuite.Tasks[1].State.ToString(),
                StateDetails = _testSuite.Tasks[1].StateDetails
            },
            new TradingTaskResponse
            {
                Id = _testSuite.Tasks[2].Id,
                StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[2].StartTimestamp),
                FinishedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[2].EndTimestamp!.Value),
                State = _testSuite.Tasks[2].State.ToString(),
                StateDetails = _testSuite.Tasks[2].StateDetails
            },
            new TradingTaskResponse
            {
                Id = _testSuite.Tasks[3].Id,
                StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[3].StartTimestamp),
                FinishedAt = null,
                State = _testSuite.Tasks[3].State.ToString(),
                StateDetails = _testSuite.Tasks[3].StateDetails
            }
        });

        await _testSuite.TradingClientSubstitute.DidNotReceive()
            .GetOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldGetTradingTasksWithQueryParameters()
    {
        await _testSuite.ResetAsync();

        using var client = _testSuite.CreateAuthenticatedClient();
        var tasks = await client.Request("api", "trading-tasks").SetQueryParams(new
        {
            start = "2023-12-07T15:37:00+00:00",
            end = "2023-12-07T15:41:00+00:00"
        }).GetJsonAsync<IReadOnlyList<TradingTaskResponse>>();
        tasks.Should().BeEquivalentTo(new[]
        {
            new TradingTaskResponse
            {
                Id = _testSuite.Tasks[1].Id,
                StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[1].StartTimestamp),
                FinishedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[1].EndTimestamp!.Value),
                State = _testSuite.Tasks[1].State.ToString(),
                StateDetails = _testSuite.Tasks[1].StateDetails
            },
            new TradingTaskResponse
            {
                Id = _testSuite.Tasks[2].Id,
                StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[2].StartTimestamp),
                FinishedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[2].EndTimestamp!.Value),
                State = _testSuite.Tasks[2].State.ToString(),
                StateDetails = _testSuite.Tasks[2].StateDetails
            }
        });
    }

    [Fact]
    public async Task ShouldValidateQueryParametersWhenRequestingTradingTasks()
    {
        using var client = _testSuite.CreateAuthenticatedClient().AllowAnyHttpStatus();
        var response = await client.Request("api", "trading-tasks")
            .SetQueryParams(new { start = "2023-12-07T15:37:00+00:00", end = "2023-12-07T14:41:00+00:00" }).GetAsync();

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var errors = await response.GetJsonAsync<ValidationProblemDetails>();
        errors.Errors.Should().ContainKey(nameof(TradingActionCollectionRequest.Start)).WhoseValue.Should()
            .ContainMatch("*must be earlier than*");
    }

    [Fact]
    public async Task ShouldGetTradingTaskById()
    {
        await _testSuite.ResetAsync();

        using var client = _testSuite.CreateAuthenticatedClient();
        var tasks = await client.Request("api", "trading-tasks", _testSuite.Tasks[3].Id)
            .GetJsonAsync<TradingTaskResponse>();
        tasks.Should().BeEquivalentTo(new TradingTaskResponse
        {
            Id = _testSuite.Tasks[3].Id,
            StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Tasks[3].StartTimestamp),
            FinishedAt = null,
            State = _testSuite.Tasks[3].State.ToString(),
            StateDetails = _testSuite.Tasks[3].StateDetails
        });
    }

    [Fact]
    public async Task ShouldReturn404NotFoundIfTaskDoesNotExist()
    {
        await _testSuite.ResetAsync();

        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "trading-tasks", Guid.NewGuid()).AllowAnyHttpStatus().GetAsync();

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ShouldGetTradingActionsForTask()
    {
        await _testSuite.ResetAsync();

        using var client = _testSuite.CreateAuthenticatedClient();
        var tasks = await client.Request("api", "trading-tasks", _testSuite.Tasks[1].Id, "trading-actions")
            .GetJsonAsync<IReadOnlyList<TradingActionResponse>>();

        var action = _testSuite.Tasks[1].TradingActions[0];
        tasks.Should().BeEquivalentTo(new[]
        {
            new TradingActionResponse
            {
                Id = action.Id,
                AlpacaId = action.AlpacaId,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(action.CreationTimestamp),
                Symbol = action.Symbol,
                OrderType = action.OrderType.ToString(),
                Quantity = (decimal)action.Quantity,
                Price = (decimal)action.Price!.Value,
                InForce = action.InForce.ToString(),
                Status = OrderStatus.Filled.ToString(),
                ExecutedAt = TradingTasksTestSuite.Now - TimeSpan.FromMinutes(8),
                AverageFillPrice = 105.32m,
                Error = null,
                TaskId = _testSuite.Tasks[1].Id
            }
        });

        await _testSuite.TradingClientSubstitute.Received(1)
            .GetOrderAsync(action.AlpacaId!.Value, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnEmptyCollectionIfTaskDoesNotHaveActions()
    {
        await _testSuite.ResetAsync();

        using var client = _testSuite.CreateAuthenticatedClient();
        var tasks = await client.Request("api", "trading-tasks", _testSuite.Tasks[0].Id, "trading-actions")
            .GetJsonAsync<IReadOnlyList<TradingActionResponse>>();

        tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldReturn404NotFoundFromActionsEndpointIfTaskDoesNotExist()
    {
        await _testSuite.ResetAsync();

        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "trading-tasks", Guid.NewGuid(), "trading-actions")
            .AllowAnyHttpStatus().GetAsync();

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
