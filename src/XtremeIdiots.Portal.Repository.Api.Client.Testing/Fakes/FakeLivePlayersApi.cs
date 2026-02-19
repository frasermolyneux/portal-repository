using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeLivePlayersApi : ILivePlayersApi
{
    private readonly ConcurrentDictionary<Guid, List<LivePlayerDto>> _livePlayers = new();

    public FakeLivePlayersApi AddLivePlayers(Guid gameServerId, List<LivePlayerDto> players) { _livePlayers[gameServerId] = players; return this; }
    public FakeLivePlayersApi Reset() { _livePlayers.Clear(); return this; }

    public Task<ApiResult<CollectionModel<LivePlayerDto>>> GetLivePlayers(GameType? gameType, Guid? gameServerId, LivePlayerFilter? filter, int skipEntries, int takeEntries, LivePlayersOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _livePlayers.Values.SelectMany(p => p).AsEnumerable();
        if (gameType.HasValue) items = items.Where(p => p.GameType == gameType.Value);
        if (gameServerId.HasValue) items = items.Where(p => p.GameServerServerId == gameServerId.Value);
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<LivePlayerDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<LivePlayerDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<LivePlayerDto>>(collection)));
    }

    public Task<ApiResult> SetLivePlayersForGameServer(Guid gameServerId, List<CreateLivePlayerDto> createLivePlayerDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
