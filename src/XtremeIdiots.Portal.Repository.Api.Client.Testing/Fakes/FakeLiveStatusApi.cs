using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeLiveStatusApi : ILiveStatusApi
{
    private readonly ConcurrentDictionary<Guid, GameServerLiveStatusDto> _statuses = new();
    private readonly ConcurrentDictionary<Guid, List<LivePlayerDto>> _players = new();

    public FakeLiveStatusApi AddStatus(GameServerLiveStatusDto status) { _statuses[status.ServerId] = status; return this; }
    public FakeLiveStatusApi AddPlayers(Guid serverId, List<LivePlayerDto> players) { _players[serverId] = players; return this; }
    public FakeLiveStatusApi Reset() { _statuses.Clear(); _players.Clear(); return this; }

    public Task<ApiResult> SetGameServerLiveStatus(Guid gameServerId, SetGameServerLiveStatusDto dto, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    }

    public Task<ApiResult<GameServerLiveStatusDto>> GetGameServerLiveStatus(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        if (_statuses.TryGetValue(gameServerId, out var status))
        {
            return Task.FromResult(new ApiResult<GameServerLiveStatusDto>(HttpStatusCode.OK, new ApiResponse<GameServerLiveStatusDto>(status)));
        }

        return Task.FromResult(new ApiResult<GameServerLiveStatusDto>(HttpStatusCode.NotFound, new ApiResponse<GameServerLiveStatusDto>(new ApiError("NotFound", "Not found"))));
    }

    public Task<ApiResult<CollectionModel<GameServerLiveStatusDto>>> GetAllGameServerLiveStatuses(CancellationToken cancellationToken = default)
    {
        var collection = new CollectionModel<GameServerLiveStatusDto> { Items = _statuses.Values.ToList() };
        return Task.FromResult(new ApiResult<CollectionModel<GameServerLiveStatusDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<GameServerLiveStatusDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<LivePlayerDto>>> GetGameServerLivePlayers(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var players = _players.TryGetValue(gameServerId, out var list) ? list : [];
        var collection = new CollectionModel<LivePlayerDto> { Items = players };
        return Task.FromResult(new ApiResult<CollectionModel<LivePlayerDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<LivePlayerDto>>(collection)));
    }
}
