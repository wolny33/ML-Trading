using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IStrategy
{
    public Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync();
}

public sealed class Strategy : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IPricePredictor _predictor;

    public Strategy(IPricePredictor predictor, IDbContextFactory<AppDbContext> dbContextFactory,
        IAssetsDataSource assetsDataSource)
    {
        _predictor = predictor;
        _dbContextFactory = dbContextFactory;
        _assetsDataSource = assetsDataSource;
    }

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync()
    {
        var actions = await DetermineTradingActionsAsync();

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var action in actions) context.TradingActions.Add(action.ToEntity());

        await context.SaveChangesAsync();

        return actions;
    }

    private async Task<IReadOnlyList<TradingAction>> DetermineTradingActionsAsync()
    {
        return await Task.FromException<IReadOnlyList<TradingAction>>(new NotImplementedException());
    }
}
