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
using TradingBot.Services;

namespace TradingBotTests;

public sealed class BacktestTestSuite : IntegrationTestSuite, IAsyncLifetime
{
    public static readonly DateTimeOffset Now = new(2024, 1, 1, 17, 20, 0, TimeSpan.Zero);

    public BacktestTestSuite()
    {
        Backtests = new[]
        {
            new BacktestEntity
            {
                Id = Guid.NewGuid(),
                State = BacktestState.Finished,
                StateDetails = "Finished successfully",
                UsePredictor = true,
                MeanPredictorError = 0,
                ExecutionStartTimestamp = (Now - TimeSpan.FromMinutes(10)).ToUnixTimeMilliseconds(),
                ExecutionEndTimestamp = (Now - TimeSpan.FromMinutes(5)).ToUnixTimeMilliseconds(),
                SimulationStart = new DateOnly(2022, 1, 1),
                SimulationEnd = new DateOnly(2022, 1, 4),
                Description = "Backtest 1",
                AssetsStates = new[]
                {
                    new AssetsStateEntity
                    {
                        Id = Guid.NewGuid(),
                        CreationTimestamp =
                            new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        AvailableCash = 100,
                        BuyingPower = 100,
                        MainCurrency = "USD",
                        EquityValue = 100,
                        Mode = Mode.Backtest
                    },
                    new AssetsStateEntity
                    {
                        Id = Guid.NewGuid(),
                        CreationTimestamp =
                            new DateTimeOffset(2022, 1, 2, 19, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        AvailableCash = 95,
                        BuyingPower = 95,
                        MainCurrency = "USD",
                        EquityValue = 105,
                        HeldPositions = new[]
                        {
                            new PositionEntity
                            {
                                Id = Guid.NewGuid(),
                                Symbol = "TKN1",
                                SymbolId = Guid.NewGuid(),
                                Quantity = 1,
                                AvailableQuantity = 1,
                                MarketValue = 10,
                                AverageEntryPrice = 5
                            }
                        },
                        Mode = Mode.Backtest
                    },
                    new AssetsStateEntity
                    {
                        Id = Guid.NewGuid(),
                        CreationTimestamp =
                            new DateTimeOffset(2022, 1, 3, 19, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        AvailableCash = 110,
                        BuyingPower = 110,
                        MainCurrency = "USD",
                        EquityValue = 120,
                        HeldPositions = new[]
                        {
                            new PositionEntity
                            {
                                Id = Guid.NewGuid(),
                                Symbol = "TKN2",
                                SymbolId = Guid.NewGuid(),
                                Quantity = 1,
                                AvailableQuantity = 1,
                                MarketValue = 10,
                                AverageEntryPrice = 5
                            }
                        },
                        Mode = Mode.Backtest
                    },
                    new AssetsStateEntity
                    {
                        Id = Guid.NewGuid(),
                        CreationTimestamp =
                            new DateTimeOffset(2022, 1, 4, 19, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        AvailableCash = 130,
                        BuyingPower = 130,
                        MainCurrency = "USD",
                        EquityValue = 130,
                        Mode = Mode.Backtest
                    }
                },
                TradingTasks = new[]
                {
                    new TradingTaskEntity
                    {
                        Id = Guid.NewGuid(),
                        State = TradingTaskState.Success,
                        StateDetails = "Finished successfully",
                        StartTimestamp =
                            new DateTimeOffset(2022, 1, 1, 20, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        EndTimestamp =
                            new DateTimeOffset(2022, 1, 1, 20, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        Mode = Mode.Backtest
                    },
                    new TradingTaskEntity
                    {
                        Id = Guid.NewGuid(),
                        State = TradingTaskState.Success,
                        StateDetails = "Finished successfully",
                        StartTimestamp =
                            new DateTimeOffset(2022, 1, 2, 20, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        EndTimestamp =
                            new DateTimeOffset(2022, 1, 2, 20, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        Mode = Mode.Backtest
                    },
                    new TradingTaskEntity
                    {
                        Id = Guid.NewGuid(),
                        State = TradingTaskState.Success,
                        StateDetails = "Finished successfully",
                        StartTimestamp =
                            new DateTimeOffset(2022, 1, 3, 20, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        EndTimestamp =
                            new DateTimeOffset(2022, 1, 3, 20, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        Mode = Mode.Backtest
                    }
                }
            },
            new BacktestEntity
            {
                Id = Guid.NewGuid(),
                State = BacktestState.Running,
                StateDetails = "Backtest is running",
                UsePredictor = true,
                MeanPredictorError = 0,
                ExecutionStartTimestamp = (Now - TimeSpan.FromMinutes(8)).ToUnixTimeMilliseconds(),
                ExecutionEndTimestamp = null,
                SimulationStart = new DateOnly(2022, 2, 1),
                SimulationEnd = new DateOnly(2023, 2, 1),
                Description = "Backtest 2",
                AssetsStates = new[]
                {
                    new AssetsStateEntity
                    {
                        Id = Guid.NewGuid(),
                        CreationTimestamp =
                            new DateTimeOffset(2022, 2, 1, 0, 0, 0, TimeSpan.Zero)
                                .ToUnixTimeMilliseconds(),
                        AvailableCash = 200,
                        BuyingPower = 200,
                        MainCurrency = "USD",
                        EquityValue = 200,
                        Mode = Mode.Backtest
                    }
                }
            },
            new BacktestEntity
            {
                Id = Guid.NewGuid(),
                State = BacktestState.Cancelled,
                StateDetails = "Backtest is running",
                UsePredictor = true,
                MeanPredictorError = 0,
                ExecutionStartTimestamp = (Now - TimeSpan.FromMinutes(7)).ToUnixTimeMilliseconds(),
                ExecutionEndTimestamp = (Now - TimeSpan.FromMinutes(6)).ToUnixTimeMilliseconds(),
                SimulationStart = new DateOnly(2002, 1, 1),
                SimulationEnd = new DateOnly(2023, 1, 1),
                Description = string.Empty
            },
            new BacktestEntity
            {
                Id = Guid.NewGuid(),
                State = BacktestState.Error,
                StateDetails =
                    "Backtest failed with error code unsuccessful-api-response: Alpaca API responded with 403: Unauthorized",
                UsePredictor = false,
                MeanPredictorError = 0.01,
                ExecutionStartTimestamp = (Now - TimeSpan.FromMinutes(5)).ToUnixTimeMilliseconds(),
                ExecutionEndTimestamp = (Now - TimeSpan.FromMinutes(4)).ToUnixTimeMilliseconds(),
                SimulationStart = new DateOnly(2020, 1, 1),
                SimulationEnd = new DateOnly(2023, 1, 1),
                Description = string.Empty
            }
        };
    }

    public TaskCompletionSource<IReadOnlyList<IAsset>> BacktestInitializationTask { get; private set; } = new();
    public IReadOnlyList<BacktestEntity> Backtests { get; }

    public Task InitializeAsync()
    {
        return ResetAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeAsync().AsTask();
    }

    public async Task ResetAsync()
    {
        if (!BacktestInitializationTask.Task.IsCompleted)
            BacktestInitializationTask.SetException(new OperationCanceledException());
        if (Services.GetRequiredService<IBacktestExecutor>() is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
        BacktestInitializationTask = new TaskCompletionSource<IReadOnlyList<IAsset>>();

        await using var context = await DbContextFactory.CreateDbContextAsync();
        await context.TradingTasks.ExecuteDeleteAsync();
        await context.AssetsStates.ExecuteDeleteAsync();
        await context.Positions.ExecuteDeleteAsync();
        await context.Backtests.ExecuteDeleteAsync();
        context.Backtests.AddRange(Backtests);
        await context.SaveChangesAsync();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<ISystemClock>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(Now);
        services.AddSingleton(clock);
    }

    protected override void SetUpAlpacaSubstitutes(IAlpacaDataClient dataClient, IAlpacaTradingClient tradingClient)
    {
        tradingClient.ListAssetsAsync(Arg.Any<AssetsRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var token = call.Arg<CancellationToken>();
                token.Register(() =>
                {
                    if (BacktestInitializationTask.Task.IsCompleted) return;

                    BacktestInitializationTask.SetException(new OperationCanceledException(token));
                });
                return BacktestInitializationTask.Task;
            });
    }
}

[Trait("Category", "Integration")]
public sealed class BacktestEndpointTests : IClassFixture<BacktestTestSuite>, IAsyncLifetime
{
    private readonly BacktestTestSuite _testSuite;

    public BacktestEndpointTests(BacktestTestSuite testSuite)
    {
        _testSuite = testSuite;
    }

    public Task InitializeAsync()
    {
        return _testSuite.ResetAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ShouldReturn401FromAllEndpointsIfUserIsUnauthorized()
    {
        using var client = _testSuite.CreateUnauthenticatedClient().AllowAnyHttpStatus();

        var backtestCollectionResponse = await client.Request("api", "backtests").GetAsync();
        backtestCollectionResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var backtestResponse = await client.Request("api", "backtests", Guid.NewGuid()).GetAsync();
        backtestResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var tasksResponse = await client.Request("api", "backtests", Guid.NewGuid(), "trading-tasks").GetAsync();
        tasksResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var assetsStatesResponse = await client.Request("api", "backtests", Guid.NewGuid(), "assets-states").GetAsync();
        assetsStatesResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var backtestCreationResponse = await client.Request("api", "backtests").PostAsync();
        backtestCreationResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var backtestCancellationResponse = await client.Request("api", "backtests", Guid.NewGuid()).DeleteAsync();
        backtestCancellationResponse.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ShouldGetAllBacktests()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests").GetJsonAsync<IReadOnlyList<BacktestResponse>>();

        response.Should().BeEquivalentTo(new[]
        {
            new BacktestResponse
            {
                Id = _testSuite.Backtests[0].Id,
                State = _testSuite.Backtests[0].State.ToString(),
                StateDetails = _testSuite.Backtests[0].StateDetails,
                SimulationStart = _testSuite.Backtests[0].SimulationStart,
                SimulationEnd = _testSuite.Backtests[0].SimulationEnd,
                ExecutionStart =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].ExecutionStartTimestamp),
                ExecutionEnd =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].ExecutionEndTimestamp!.Value),
                TotalReturn = 0.3,
                UsePredictor = _testSuite.Backtests[0].UsePredictor,
                MeanPredictorError = _testSuite.Backtests[0].MeanPredictorError,
                Description = _testSuite.Backtests[0].Description
            },
            new BacktestResponse
            {
                Id = _testSuite.Backtests[1].Id,
                State = _testSuite.Backtests[1].State.ToString(),
                StateDetails = _testSuite.Backtests[1].StateDetails,
                SimulationStart = _testSuite.Backtests[1].SimulationStart,
                SimulationEnd = _testSuite.Backtests[1].SimulationEnd,
                ExecutionStart =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[1].ExecutionStartTimestamp),
                ExecutionEnd = null,
                TotalReturn = 0,
                UsePredictor = _testSuite.Backtests[1].UsePredictor,
                MeanPredictorError = _testSuite.Backtests[1].MeanPredictorError,
                Description = _testSuite.Backtests[1].Description
            },
            new BacktestResponse
            {
                Id = _testSuite.Backtests[2].Id,
                State = _testSuite.Backtests[2].State.ToString(),
                StateDetails = _testSuite.Backtests[2].StateDetails,
                SimulationStart = _testSuite.Backtests[2].SimulationStart,
                SimulationEnd = _testSuite.Backtests[2].SimulationEnd,
                ExecutionStart =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[2].ExecutionStartTimestamp),
                ExecutionEnd =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[2].ExecutionEndTimestamp!.Value),
                TotalReturn = 0,
                UsePredictor = _testSuite.Backtests[2].UsePredictor,
                MeanPredictorError = _testSuite.Backtests[2].MeanPredictorError,
                Description = _testSuite.Backtests[2].Description
            },
            new BacktestResponse
            {
                Id = _testSuite.Backtests[3].Id,
                State = _testSuite.Backtests[3].State.ToString(),
                StateDetails = _testSuite.Backtests[3].StateDetails,
                SimulationStart = _testSuite.Backtests[3].SimulationStart,
                SimulationEnd = _testSuite.Backtests[3].SimulationEnd,
                ExecutionStart =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[3].ExecutionStartTimestamp),
                ExecutionEnd =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[3].ExecutionEndTimestamp!.Value),
                TotalReturn = 0,
                UsePredictor = _testSuite.Backtests[3].UsePredictor,
                MeanPredictorError = _testSuite.Backtests[3].MeanPredictorError,
                Description = _testSuite.Backtests[3].Description
            }
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ShouldGetBacktestsWithQueryParameters()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests")
            .SetQueryParams(new { start = "2024-01-01T17:11:00+00:00", end = "2024-01-01T17:14:00+00:00" })
            .GetJsonAsync<IReadOnlyList<BacktestResponse>>();

        response.Should().BeEquivalentTo(new[]
        {
            new BacktestResponse
            {
                Id = _testSuite.Backtests[1].Id,
                State = _testSuite.Backtests[1].State.ToString(),
                StateDetails = _testSuite.Backtests[1].StateDetails,
                SimulationStart = _testSuite.Backtests[1].SimulationStart,
                SimulationEnd = _testSuite.Backtests[1].SimulationEnd,
                ExecutionStart =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[1].ExecutionStartTimestamp),
                ExecutionEnd = null,
                TotalReturn = 0,
                UsePredictor = _testSuite.Backtests[1].UsePredictor,
                MeanPredictorError = _testSuite.Backtests[1].MeanPredictorError,
                Description = _testSuite.Backtests[1].Description
            },
            new BacktestResponse
            {
                Id = _testSuite.Backtests[2].Id,
                State = _testSuite.Backtests[2].State.ToString(),
                StateDetails = _testSuite.Backtests[2].StateDetails,
                SimulationStart = _testSuite.Backtests[2].SimulationStart,
                SimulationEnd = _testSuite.Backtests[2].SimulationEnd,
                ExecutionStart =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[2].ExecutionStartTimestamp),
                ExecutionEnd =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[2].ExecutionEndTimestamp!.Value),
                TotalReturn = 0,
                UsePredictor = _testSuite.Backtests[2].UsePredictor,
                MeanPredictorError = _testSuite.Backtests[2].MeanPredictorError,
                Description = _testSuite.Backtests[2].Description
            }
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ShouldValidateQueryParametersWhenRequestingBacktests()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests")
            .AllowAnyHttpStatus()
            .SetQueryParams(new { start = "2024-01-02", end = "2024-01-01" })
            .GetAsync();

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var error = await response.GetJsonAsync<ValidationProblemDetails>();
        error.Errors.Should().ContainKey("Start").WhoseValue.Should().ContainMatch("*must be earlier than*");
    }

    [Fact]
    public async Task ShouldGetBacktestById()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests", _testSuite.Backtests[0].Id)
            .GetJsonAsync<BacktestResponse>();

        response.Should().BeEquivalentTo(new BacktestResponse
        {
            Id = _testSuite.Backtests[0].Id,
            State = _testSuite.Backtests[0].State.ToString(),
            StateDetails = _testSuite.Backtests[0].StateDetails,
            SimulationStart = _testSuite.Backtests[0].SimulationStart,
            SimulationEnd = _testSuite.Backtests[0].SimulationEnd,
            ExecutionStart =
                DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].ExecutionStartTimestamp),
            ExecutionEnd =
                DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].ExecutionEndTimestamp!.Value),
            TotalReturn = 0.3,
            UsePredictor = _testSuite.Backtests[0].UsePredictor,
            MeanPredictorError = _testSuite.Backtests[0].MeanPredictorError,
            Description = _testSuite.Backtests[0].Description
        });
    }

    [Fact]
    public async Task ShouldReturn404NotFoundIfBacktestDoesNotExist()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests", Guid.NewGuid()).AllowAnyHttpStatus().GetAsync();

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ShouldGetTradingTasksForBacktest()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests", _testSuite.Backtests[0].Id, "trading-tasks")
            .GetJsonAsync<IReadOnlyList<TradingTaskResponse>>();

        response.Should().BeEquivalentTo(new[]
        {
            new TradingTaskResponse
            {
                Id = _testSuite.Backtests[0].TradingTasks[0].Id,
                BacktestId = _testSuite.Backtests[0].Id,
                State = _testSuite.Backtests[0].TradingTasks[0].State.ToString(),
                StateDetails = _testSuite.Backtests[0].TradingTasks[0].StateDetails,
                StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].TradingTasks[0]
                    .StartTimestamp),
                FinishedAt =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].TradingTasks[0].EndTimestamp!.Value)
            },
            new TradingTaskResponse
            {
                Id = _testSuite.Backtests[0].TradingTasks[1].Id,
                BacktestId = _testSuite.Backtests[0].Id,
                State = _testSuite.Backtests[0].TradingTasks[1].State.ToString(),
                StateDetails = _testSuite.Backtests[0].TradingTasks[1].StateDetails,
                StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].TradingTasks[1]
                    .StartTimestamp),
                FinishedAt =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].TradingTasks[1].EndTimestamp!.Value)
            },
            new TradingTaskResponse
            {
                Id = _testSuite.Backtests[0].TradingTasks[2].Id,
                BacktestId = _testSuite.Backtests[0].Id,
                State = _testSuite.Backtests[0].TradingTasks[2].State.ToString(),
                StateDetails = _testSuite.Backtests[0].TradingTasks[2].StateDetails,
                StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].TradingTasks[2]
                    .StartTimestamp),
                FinishedAt =
                    DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].TradingTasks[2].EndTimestamp!.Value)
            }
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ShouldReturn404NotFoundFromTradingTasksEndpointIfBacktestDoesNotExist()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests", Guid.NewGuid(), "trading-tasks").AllowAnyHttpStatus()
            .GetAsync();

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ShouldGetAssetsStatesForBacktest()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests", _testSuite.Backtests[0].Id, "assets-states")
            .GetJsonAsync<IReadOnlyList<AssetsStateResponse>>();

        response.Should().BeEquivalentTo(new[]
        {
            new AssetsStateResponse
            {
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].AssetsStates[0]
                    .CreationTimestamp),
                Assets = new AssetsResponse
                {
                    EquityValue = (decimal)_testSuite.Backtests[0].AssetsStates[0].EquityValue,
                    Cash = new CashResponse
                    {
                        MainCurrency = _testSuite.Backtests[0].AssetsStates[0].MainCurrency,
                        AvailableAmount = (decimal)_testSuite.Backtests[0].AssetsStates[0].AvailableCash,
                        BuyingPower = (decimal)_testSuite.Backtests[0].AssetsStates[0].BuyingPower
                    },
                    Positions = Array.Empty<PositionResponse>()
                }
            },
            new AssetsStateResponse
            {
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].AssetsStates[1]
                    .CreationTimestamp),
                Assets = new AssetsResponse
                {
                    EquityValue = (decimal)_testSuite.Backtests[0].AssetsStates[1].EquityValue,
                    Cash = new CashResponse
                    {
                        MainCurrency = _testSuite.Backtests[0].AssetsStates[1].MainCurrency,
                        AvailableAmount = (decimal)_testSuite.Backtests[0].AssetsStates[1].AvailableCash,
                        BuyingPower = (decimal)_testSuite.Backtests[0].AssetsStates[1].BuyingPower
                    },
                    Positions = new[]
                    {
                        new PositionResponse
                        {
                            Symbol = _testSuite.Backtests[0].AssetsStates[1].HeldPositions[0].Symbol,
                            Quantity = (decimal)_testSuite.Backtests[0].AssetsStates[1].HeldPositions[0].Quantity,
                            AvailableQuantity = (decimal)_testSuite.Backtests[0].AssetsStates[1].HeldPositions[0]
                                .AvailableQuantity,
                            AverageEntryPrice = (decimal)_testSuite.Backtests[0].AssetsStates[1].HeldPositions[0]
                                .AverageEntryPrice,
                            MarketValue = (decimal)_testSuite.Backtests[0].AssetsStates[1].HeldPositions[0].MarketValue
                        }
                    }
                }
            },
            new AssetsStateResponse
            {
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].AssetsStates[2]
                    .CreationTimestamp),
                Assets = new AssetsResponse
                {
                    EquityValue = (decimal)_testSuite.Backtests[0].AssetsStates[2].EquityValue,
                    Cash = new CashResponse
                    {
                        MainCurrency = _testSuite.Backtests[0].AssetsStates[2].MainCurrency,
                        AvailableAmount = (decimal)_testSuite.Backtests[0].AssetsStates[2].AvailableCash,
                        BuyingPower = (decimal)_testSuite.Backtests[0].AssetsStates[2].BuyingPower
                    },
                    Positions = new[]
                    {
                        new PositionResponse
                        {
                            Symbol = _testSuite.Backtests[0].AssetsStates[2].HeldPositions[0].Symbol,
                            Quantity = (decimal)_testSuite.Backtests[0].AssetsStates[2].HeldPositions[0].Quantity,
                            AvailableQuantity = (decimal)_testSuite.Backtests[0].AssetsStates[2].HeldPositions[0]
                                .AvailableQuantity,
                            AverageEntryPrice = (decimal)_testSuite.Backtests[0].AssetsStates[2].HeldPositions[0]
                                .AverageEntryPrice,
                            MarketValue = (decimal)_testSuite.Backtests[0].AssetsStates[2].HeldPositions[0].MarketValue
                        }
                    }
                }
            },
            new AssetsStateResponse
            {
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(_testSuite.Backtests[0].AssetsStates[3]
                    .CreationTimestamp),
                Assets = new AssetsResponse
                {
                    EquityValue = (decimal)_testSuite.Backtests[0].AssetsStates[3].EquityValue,
                    Cash = new CashResponse
                    {
                        MainCurrency = _testSuite.Backtests[0].AssetsStates[3].MainCurrency,
                        AvailableAmount = (decimal)_testSuite.Backtests[0].AssetsStates[3].AvailableCash,
                        BuyingPower = (decimal)_testSuite.Backtests[0].AssetsStates[3].BuyingPower
                    },
                    Positions = Array.Empty<PositionResponse>()
                }
            }
        });
    }

    [Fact]
    public async Task ShouldReturn404NotFoundFromAssetsStatesEndpointIfBacktestDoesNotExist()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests", Guid.NewGuid(), "assets-states").AllowAnyHttpStatus()
            .GetAsync();

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ShouldStartNewBacktest()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests").PostJsonAsync(new
        {
            start = "2022-01-01",
            end = "2023-01-01",
            initialCash = 10_000,
            shouldUsePredictor = true
        });

        response.StatusCode.Should().Be(StatusCodes.Status202Accepted);

        var id = Guid.Empty;
        response.Headers.Should().ContainSingle(pair => pair.Name == "Location").Which.Value.Should()
            .Match(v => Guid.TryParse(v, out id));

        await using var context = await _testSuite.DbContextFactory.CreateDbContextAsync();
        context.Backtests
            .Include(b => b.TradingTasks)
            .Include(b => b.AssetsStates)
            .ThenInclude(a => a.HeldPositions)
            .Single(b => b.Id == id)
            .Should()
            .BeEquivalentTo(new BacktestEntity
            {
                Id = id,
                State = BacktestState.Running,
                StateDetails = "Backtest is running",
                SimulationStart = new DateOnly(2022, 1, 1),
                SimulationEnd = new DateOnly(2023, 1, 1),
                ExecutionStartTimestamp = BacktestTestSuite.Now.ToUnixTimeMilliseconds(),
                ExecutionEndTimestamp = null,
                UsePredictor = true,
                MeanPredictorError = 0,
                Description = string.Empty,
                TradingTasks = Array.Empty<TradingTaskEntity>(),
                AssetsStates = new[]
                {
                    new AssetsStateEntity
                    {
                        Id = Guid.NewGuid(),
                        BacktestId = id,
                        MainCurrency = "USD",
                        AvailableCash = 10_000,
                        BuyingPower = 10_000,
                        EquityValue = 10_000,
                        CreationTimestamp = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero)
                            .ToUnixTimeMilliseconds(),
                        HeldPositions = Array.Empty<PositionEntity>(),
                        Mode = Mode.Backtest
                    }
                }
            }, options => options.For(b => b.AssetsStates)
                .Exclude(a => a.Id)
                .For(b => b.AssetsStates)
                .Exclude(a => a.Backtest));
    }

    [Fact]
    public async Task ShouldValidateBacktestCreationRequest()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var firstResponse = await client.Request("api", "backtests").AllowAnyHttpStatus().PostJsonAsync(new
        {
            start = "2023-01-01",
            end = "2022-01-01",
            initialCash = 10_000
        });

        firstResponse.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var firstError = await firstResponse.GetJsonAsync<ValidationProblemDetails>();
        firstError.Errors.Should().ContainKey("Start").WhoseValue.Should().ContainMatch("*must be earlier than*");

        var secondResponse = await client.Request("api", "backtests").AllowAnyHttpStatus().PostJsonAsync(new
        {
            start = "2022-01-01",
            end = "2023-01-01",
            initialCash = 0
        });

        secondResponse.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var secondError = await secondResponse.GetJsonAsync<ValidationProblemDetails>();
        secondError.Errors.Should().ContainKey("InitialCash").WhoseValue.Should().ContainMatch("*must be positive*");

        var thirdResponse = await client.Request("api", "backtests").AllowAnyHttpStatus().PostJsonAsync(new
        {
            start = "2022-01-01",
            end = "2024-01-01",
            initialCash = 10_000
        });

        thirdResponse.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var thirdError = await thirdResponse.GetJsonAsync<ValidationProblemDetails>();
        thirdError.Errors.Should().ContainKey("End").WhoseValue.Should().ContainMatch("*must represent a past day*");
    }

    [Fact]
    public async Task ShouldCancelBacktestAfterStarting()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var creationResponse = await client.Request("api", "backtests").PostJsonAsync(new
        {
            start = "2020-01-01",
            end = "2021-01-01",
            initialCash = 1000
        });

        creationResponse.StatusCode.Should().Be(StatusCodes.Status202Accepted);

        var id = Guid.Empty;
        creationResponse.Headers.Should().ContainSingle(pair => pair.Name == "Location").Which.Value.Should()
            .Match(v => Guid.TryParse(v, out id));

        await using (var context = await _testSuite.DbContextFactory.CreateDbContextAsync())
        {
            context.Backtests.Should().ContainSingle(b => b.Id == id).Which.State.Should().Be(BacktestState.Running);
        }

        var cancellationResponse = await client.Request("api", "backtests", id).DeleteAsync();

        cancellationResponse.StatusCode.Should().Be(StatusCodes.Status204NoContent);

        await using (var context = await _testSuite.DbContextFactory.CreateDbContextAsync())
        {
            context.Backtests.Should().ContainSingle(b => b.Id == id).Which.State.Should().Be(BacktestState.Cancelled);
        }
    }

    [Fact]
    public async Task ShouldReturn204NoContentWhenAttemptingToCancelNonExistentBacktest()
    {
        using var client = _testSuite.CreateAuthenticatedClient();
        var response = await client.Request("api", "backtests", Guid.NewGuid()).DeleteAsync();

        response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }
}
