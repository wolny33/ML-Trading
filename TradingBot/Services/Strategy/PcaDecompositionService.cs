using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public interface IPcaDecompositionService
{
    Task<PcaDecomposition?> GetLatestDecompositionAsync(Guid? backtestId, CancellationToken token = default);
    Task SaveDecompositionAsync(PcaDecomposition decomposition, Guid? backtestId, CancellationToken token = default);
}

public sealed class PcaDecompositionService : IPcaDecompositionService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public PcaDecompositionService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public Task<PcaDecomposition?> GetLatestDecompositionAsync(Guid? backtestId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task SaveDecompositionAsync(PcaDecomposition decomposition, Guid? backtestId,
        CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
