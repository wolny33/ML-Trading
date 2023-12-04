using Flurl.Http;

namespace TradingBot.Services.AlpacaClients;

public interface IAlpacaAssetsClient : IDisposable
{
    Task<IReadOnlyList<AssetResponse>> GetAvailableAssetsAsync(CancellationToken token = default);
}

public sealed class AlpacaAssetsClient : IAlpacaAssetsClient
{
    private readonly IFlurlClient _client;

    public AlpacaAssetsClient(IFlurlClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<AssetResponse>> GetAvailableAssetsAsync(CancellationToken token = default)
    {
        var response = await _client.Request().AllowAnyHttpStatus().GetAsync(token);
        return response.StatusCode switch
        {
            StatusCodes.Status200OK => await response.GetJsonAsync<IReadOnlyList<AssetResponse>>(),
            var status => throw new UnsuccessfulAlpacaResponseException(status, await response.GetStringAsync())
        };
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
