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
using TradingBot.Exceptions;
using TradingBot.Models;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBotTests;

public sealed class PerformanceTestSuite : IntegrationTestSuite, IAsyncLifetime
{
    public static readonly DateTimeOffset Now = new(2023, 12, 1, 18, 11, 0, TimeSpan.Zero);

    public PerformanceTestSuite()
    {
        Actions = new[]
        {
            new TradingActionEntity
            {
                Id = Guid.NewGuid(),
                AlpacaId = Guid.NewGuid(),
                CreationTimestamp = (Now - TimeSpan.FromMinutes(20)).ToUnixTimeMilliseconds(),
                Symbol = "AMZN",
                OrderType = OrderType.LimitBuy,
                Quantity = 12.5,
                Price = 105.5,
                InForce = TimeInForce.Day,
                Status = OrderStatus.Filled,
                ExecutionTimestamp = (Now - TimeSpan.FromMinutes(18)).ToUnixTimeMilliseconds(),
                AverageFillPrice = 105.34,
                ErrorCode = null,
                ErrorMessage = null,
                TradingTaskId = Task.Id
            },
            new TradingActionEntity
            {
                Id = Guid.NewGuid(),
                AlpacaId = Guid.NewGuid(),
                CreationTimestamp = (Now - TimeSpan.FromMinutes(18)).ToUnixTimeMilliseconds(),
                Symbol = "TSLA",
                OrderType = OrderType.MarketSell,
                Quantity = 34.4,
                Price = null,
                InForce = TimeInForce.Day,
                Status = OrderStatus.Filled,
                ExecutionTimestamp = (Now - TimeSpan.FromMinutes(17)).ToUnixTimeMilliseconds(),
                AverageFillPrice = 56.7,
                ErrorCode = null,
                ErrorMessage = null,
                TradingTaskId = Task.Id
            },
            new TradingActionEntity
            {
                Id = Guid.NewGuid(),
                AlpacaId = Guid.NewGuid(),
                CreationTimestamp = (Now - TimeSpan.FromMinutes(15)).ToUnixTimeMilliseconds(),
                Symbol = "TQQQ",
                OrderType = OrderType.LimitBuy,
                Quantity = 23.8,
                Price = 33.4,
                InForce = TimeInForce.Day,
                Status = OrderStatus.Canceled,
                ExecutionTimestamp = (Now - TimeSpan.FromMinutes(13)).ToUnixTimeMilliseconds(),
                AverageFillPrice = null,
                ErrorCode = null,
                ErrorMessage = null,
                TradingTaskId = Task.Id
            },
            new TradingActionEntity
            {
                Id = Guid.NewGuid(),
                AlpacaId = Guid.NewGuid(),
                CreationTimestamp = (Now - TimeSpan.FromMinutes(13)).ToUnixTimeMilliseconds(),
                Symbol = "F",
                OrderType = OrderType.LimitSell,
                Quantity = 35.8,
                Price = 30,
                InForce = TimeInForce.Day,
                Status = OrderStatus.PartiallyFilled,
                ExecutionTimestamp = null,
                AverageFillPrice = 30.4,
                ErrorCode = null,
                ErrorMessage = null,
                TradingTaskId = Task.Id
            },
            new TradingActionEntity
            {
                Id = Guid.NewGuid(),
                AlpacaId = null,
                CreationTimestamp = (Now - TimeSpan.FromMinutes(12)).ToUnixTimeMilliseconds(),
                Symbol = "AMZN",
                OrderType = OrderType.MarketSell,
                Quantity = 35.8,
                Price = null,
                InForce = TimeInForce.Day,
                Status = null,
                ExecutionTimestamp = null,
                AverageFillPrice = null,
                ErrorCode = "insufficient-assets",
                ErrorMessage = "Requested asset amount in sell order is greater than available amount",
                TradingTaskId = Task.Id
            }
        };
    }

    public TradingTaskEntity Task { get; } = new()
    {
        Id = Guid.NewGuid(),
        StartTimestamp = new DateTimeOffset(2024, 2, 21, 14, 43, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
        EndTimestamp = null,
        State = TradingTaskState.Success,
        StateDetails = "Finished successfully",
        BacktestId = null,
        Mode = Mode.TestMode
    };

    public IReadOnlyList<TradingActionEntity> Actions { get; }

    public IReadOnlyList<AssetsStateEntity> AssetsStates { get; } = new[]
    {
        new AssetsStateEntity
        {
            Id = Guid.NewGuid(),
            CreationTimestamp = (Now - TimeSpan.FromDays(4)).ToUnixTimeMilliseconds(),
            MainCurrency = "USD",
            AvailableCash = 1000,
            BuyingPower = 4000,
            EquityValue = 2000,
            Mode = Mode.LiveTrading
        },
        new AssetsStateEntity
        {
            Id = Guid.NewGuid(),
            CreationTimestamp = (Now - TimeSpan.FromDays(3)).ToUnixTimeMilliseconds(),
            MainCurrency = "USD",
            AvailableCash = 100,
            BuyingPower = 400,
            EquityValue = 200,
            Mode = Mode.TestMode
        },
        new AssetsStateEntity
        {
            Id = Guid.NewGuid(),
            CreationTimestamp = (Now - TimeSpan.FromDays(2)).ToUnixTimeMilliseconds(),
            MainCurrency = "USD",
            AvailableCash = 100,
            BuyingPower = 400,
            EquityValue = 180,
            Mode = Mode.TestMode
        },
        new AssetsStateEntity
        {
            Id = Guid.NewGuid(),
            CreationTimestamp = (Now - TimeSpan.FromDays(1)).ToUnixTimeMilliseconds(),
            MainCurrency = "USD",
            AvailableCash = 100,
            BuyingPower = 400,
            EquityValue = 220,
            Mode = Mode.TestMode
        }
    };

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
        order.AverageFillPrice.Returns(30.2m);
        order.FilledAtUtc.Returns((Now - TimeSpan.FromMinutes(10)).DateTime);
        tradingClient.GetOrderAsync(Actions[3].AlpacaId!.Value, Arg.Any<CancellationToken>()).Returns(order);
    }

    public async Task ResetAsync()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        await context.TradingActions.ExecuteDeleteAsync();
        await context.TradingTasks.ExecuteDeleteAsync();
        await context.AssetsStates.ExecuteDeleteAsync();
        context.TradingTasks.Add(Task);
        context.TradingActions.AddRange(Actions);
        context.AssetsStates.AddRange(AssetsStates);
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
public sealed class PerformanceEndpointTests : IClassFixture<PerformanceTestSuite>
{
    private readonly PerformanceTestSuite _testSuite;

    public PerformanceEndpointTests(PerformanceTestSuite testSuite)
    {
        _testSuite = testSuite;
    }

    [Fact]
    public async Task ShouldReturn401FromAllEndpointsIfUserIsUnauthorized()
    {
        using var client = _testSuite.CreateUnauthenticatedClient().AllowAnyHttpStatus();

        var performanceResponse = await client.Request("api", "performance").GetAsync();
        performanceResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var actionsResponse = await client.Request("api", "performance", "trading-actions").GetAsync();
        actionsResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var detailsResponse = await client.Request("api", "performance", "trading-actions", Guid.NewGuid()).GetAsync();
        detailsResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ShouldReturnCorrectReturnsData()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var returns = await client.Request("api", "performance").GetJsonAsync<IReadOnlyList<ReturnResponse>>();

        returns.Should().BeEquivalentTo(new[]
        {
            new ReturnResponse
            {
                Return = 0,
                Time = PerformanceTestSuite.Now - TimeSpan.FromDays(3)
            },
            new ReturnResponse
            {
                Return = -0.1m,
                Time = PerformanceTestSuite.Now - TimeSpan.FromDays(2)
            },
            new ReturnResponse
            {
                Return = 0.1m,
                Time = PerformanceTestSuite.Now - TimeSpan.FromDays(1)
            }
        });
    }

    [Fact]
    public async Task ShouldReturnReturnsDataWhenUsingQueryParameters()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.AllowAnyHttpStatus().Request("api", "performance")
            .SetQueryParams(new
            {
                start = "2023-11-29T17:00:00",
                end = "2023-12-01T19:00:00"
            }).GetAsync();
        var returns = await response.GetJsonAsync<IReadOnlyList<ReturnResponse>>();

        returns.Should().BeEquivalentTo(new[]
        {
            new ReturnResponse
            {
                Return = -0.1m,
                Time = PerformanceTestSuite.Now - TimeSpan.FromDays(2)
            },
            new ReturnResponse
            {
                Return = 0.1m,
                Time = PerformanceTestSuite.Now - TimeSpan.FromDays(1)
            }
        });
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
    public async Task ShouldReturnTradeActions()
    {
        await _testSuite.ResetAsync();

        using var client = _testSuite.CreateAuthenticatedClient();
        var actions = await client.Request("api", "performance", "trading-actions")
            .GetJsonAsync<IReadOnlyList<TradingActionResponse>>();

        actions.Should().BeEquivalentTo(new[]
        {
            new TradingActionResponse
            {
                Id = _testSuite.Actions[0].Id,
                AlpacaId = _testSuite.Actions[0].AlpacaId,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[0].CreationTimestamp),
                Symbol = _testSuite.Actions[0].Symbol,
                OrderType = _testSuite.Actions[0].OrderType.ToString(),
                Quantity = (decimal)_testSuite.Actions[0].Quantity,
                Price = (decimal)_testSuite.Actions[0].Price!.Value,
                InForce = _testSuite.Actions[0].InForce.ToString(),
                Status = _testSuite.Actions[0].Status.ToString()!,
                ExecutedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[0].ExecutionTimestamp!.Value),
                AverageFillPrice = (decimal)_testSuite.Actions[0].AverageFillPrice!.Value,
                Error = null,
                TaskId = _testSuite.Task.Id
            },
            new TradingActionResponse
            {
                Id = _testSuite.Actions[1].Id,
                AlpacaId = _testSuite.Actions[1].AlpacaId,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[1].CreationTimestamp),
                Symbol = _testSuite.Actions[1].Symbol,
                OrderType = _testSuite.Actions[1].OrderType.ToString(),
                Quantity = (decimal)_testSuite.Actions[1].Quantity,
                Price = null,
                InForce = _testSuite.Actions[1].InForce.ToString(),
                Status = _testSuite.Actions[1].Status.ToString()!,
                ExecutedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[1].ExecutionTimestamp!.Value),
                AverageFillPrice = (decimal)_testSuite.Actions[1].AverageFillPrice!.Value,
                Error = null,
                TaskId = _testSuite.Task.Id
            },
            new TradingActionResponse
            {
                Id = _testSuite.Actions[2].Id,
                AlpacaId = _testSuite.Actions[2].AlpacaId,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[2].CreationTimestamp),
                Symbol = _testSuite.Actions[2].Symbol,
                OrderType = _testSuite.Actions[2].OrderType.ToString(),
                Quantity = (decimal)_testSuite.Actions[2].Quantity,
                Price = (decimal)_testSuite.Actions[2].Price!.Value,
                InForce = _testSuite.Actions[2].InForce.ToString(),
                Status = _testSuite.Actions[2].Status.ToString()!,
                ExecutedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[2].ExecutionTimestamp!.Value),
                AverageFillPrice = null,
                Error = null,
                TaskId = _testSuite.Task.Id
            },
            new TradingActionResponse
            {
                Id = _testSuite.Actions[3].Id,
                AlpacaId = _testSuite.Actions[3].AlpacaId,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[3].CreationTimestamp),
                Symbol = _testSuite.Actions[3].Symbol,
                OrderType = _testSuite.Actions[3].OrderType.ToString(),
                Quantity = (decimal)_testSuite.Actions[3].Quantity,
                Price = (decimal)_testSuite.Actions[3].Price!.Value,
                InForce = _testSuite.Actions[3].InForce.ToString(),
                Status = OrderStatus.Filled.ToString(),
                ExecutedAt = PerformanceTestSuite.Now - TimeSpan.FromMinutes(10),
                AverageFillPrice = 30.2m,
                Error = null,
                TaskId = _testSuite.Task.Id
            },
            new TradingActionResponse
            {
                Id = _testSuite.Actions[4].Id,
                AlpacaId = _testSuite.Actions[4].AlpacaId,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[4].CreationTimestamp),
                Symbol = _testSuite.Actions[4].Symbol,
                OrderType = _testSuite.Actions[4].OrderType.ToString(),
                Quantity = (decimal)_testSuite.Actions[4].Quantity,
                Price = null,
                InForce = _testSuite.Actions[4].InForce.ToString(),
                Status = "NotPosted",
                ExecutedAt = null,
                AverageFillPrice = null,
                Error = new ErrorResponse
                {
                    Code = _testSuite.Actions[4].ErrorCode!,
                    Message = _testSuite.Actions[4].ErrorMessage!
                },
                TaskId = _testSuite.Task.Id
            }
        });

        await _testSuite.TradingClientSubstitute.Received(1)
            .GetOrderAsync(_testSuite.Actions[3].AlpacaId!.Value, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnTradeActionsWhenUsingQueryParameters()
    {
        await _testSuite.ResetAsync();

        using var client = _testSuite.CreateAuthenticatedClient();
        var actions = await client.Request("api", "performance", "trading-actions")
            .SetQueryParams(new
            {
                start = "2023-12-01T17:52:00+00:00",
                end = "2023-12-01T17:57:00+00:00"
            }).GetJsonAsync<IReadOnlyList<TradingActionResponse>>();

        actions.Should().BeEquivalentTo(new[]
        {
            new TradingActionResponse
            {
                Id = _testSuite.Actions[1].Id,
                AlpacaId = _testSuite.Actions[1].AlpacaId,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[1].CreationTimestamp),
                Symbol = _testSuite.Actions[1].Symbol,
                OrderType = _testSuite.Actions[1].OrderType.ToString(),
                Quantity = (decimal)_testSuite.Actions[1].Quantity,
                Price = null,
                InForce = _testSuite.Actions[1].InForce.ToString(),
                Status = _testSuite.Actions[1].Status.ToString()!,
                ExecutedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[1].ExecutionTimestamp!.Value),
                AverageFillPrice = (decimal)_testSuite.Actions[1].AverageFillPrice!.Value,
                Error = null,
                TaskId = _testSuite.Task.Id
            },
            new TradingActionResponse
            {
                Id = _testSuite.Actions[2].Id,
                AlpacaId = _testSuite.Actions[2].AlpacaId,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[2].CreationTimestamp),
                Symbol = _testSuite.Actions[2].Symbol,
                OrderType = _testSuite.Actions[2].OrderType.ToString(),
                Quantity = (decimal)_testSuite.Actions[2].Quantity,
                Price = (decimal)_testSuite.Actions[2].Price!.Value,
                InForce = _testSuite.Actions[2].InForce.ToString(),
                Status = _testSuite.Actions[2].Status.ToString()!,
                ExecutedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Actions[2].ExecutionTimestamp!.Value),
                AverageFillPrice = null,
                Error = null,
                TaskId = _testSuite.Task.Id
            }
        });

        await _testSuite.TradingClientSubstitute.DidNotReceive()
            .GetOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldValidateQueryParametersWhenRequestingTradeActions()
    {
        using var client = _testSuite.CreateAuthenticatedClient().AllowAnyHttpStatus();
        var response = await client.Request("api", "performance", "trading-actions")
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
}
