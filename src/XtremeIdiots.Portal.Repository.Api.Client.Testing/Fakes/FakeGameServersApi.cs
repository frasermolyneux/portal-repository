using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeGameServersApi : IGameServersApi
{
    private readonly ConcurrentDictionary<Guid, GameServerDto> _gameServers = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);

    public FakeGameServersApi AddGameServer(GameServerDto gameServer) { _gameServers[gameServer.GameServerId] = gameServer; return this; }
    public FakeGameServersApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeGameServersApi Reset() { _gameServers.Clear(); _errorResponses.Clear(); return this; }

    public Task<ApiResult<GameServerDto>> GetGameServer(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        if (_gameServers.TryGetValue(gameServerId, out var gs))
            return Task.FromResult(new ApiResult<GameServerDto>(HttpStatusCode.OK, new ApiResponse<GameServerDto>(gs)));
        return Task.FromResult(new ApiResult<GameServerDto>(HttpStatusCode.NotFound, new ApiResponse<GameServerDto>(new ApiError("NOT_FOUND", "Game server not found"))));
    }

    public Task<ApiResult<CollectionModel<GameServerDto>>> GetGameServers(GameType[]? gameTypes, Guid[]? gameServerIds, GameServerFilter? filter, int skipEntries, int takeEntries, GameServerOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _gameServers.Values.AsEnumerable();
        if (gameTypes != null) items = items.Where(gs => gameTypes.Contains(gs.GameType));
        if (gameServerIds != null) items = items.Where(gs => gameServerIds.Contains(gs.GameServerId));
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<GameServerDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<GameServerDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<GameServerDto>>(collection)));
    }

    public Task<ApiResult> CreateGameServer(CreateGameServerDto createGameServerDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CreateGameServers(List<CreateGameServerDto> createGameServerDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateGameServer(EditGameServerDto editGameServerDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteGameServer(Guid gameServerId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
