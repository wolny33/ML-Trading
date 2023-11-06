using Alpaca.Markets;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IActionExecutor
{
    public Task ExecuteTradingActionsAsync();
}

public sealed class ActionExecutor : IActionExecutor
{
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly IStrategy _strategy;

    public ActionExecutor(IStrategy strategy, IAlpacaClientFactory clientFactory)
    {
        _strategy = strategy;
        _clientFactory = clientFactory;
    }

    public async Task ExecuteTradingActionsAsync()
    {
        var actions = await _strategy.GetTradingActionsAsync();
        var client = _clientFactory.CreateTradingClient();
        foreach (var action in actions) await ExecuteActionAsync(action, client);
    }

    private async Task ExecuteActionAsync(TradingAction action, IAlpacaTradingClient client)
    {
        await Task.FromException(new NotImplementedException());
    }
}
