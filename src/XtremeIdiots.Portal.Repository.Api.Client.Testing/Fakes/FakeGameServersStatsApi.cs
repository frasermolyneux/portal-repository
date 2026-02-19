using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeGameServersStatsApi : IGameServersStatsApi
{
    private readonly ConcurrentDictionary<Guid, List<GameServerStatDto>> _stats = new();

    public FakeGameServersStatsApi AddGameServerStats(Guid gameServerId, List<GameServerStatDto> stats) { _stats[gameServerId] = stats; return this; }
    public FakeGameServersStatsApi Reset() { _stats.Clear(); return this; }

    public Task<ApiResult> CreateGameServerStats(List<CreateGameServerStatDto> createGameServerStatDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    public Task<ApiResult<CollectionModel<GameServerStatDto>>> GetGameServerStatusStats(Guid gameServerId, DateTime cutoff, CancellationToken cancellationToken = default)
    {
        var items = _stats.TryGetValue(gameServerId, out var stats) ? stats : [];
        var collection = new CollectionModel<GameServerStatDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<GameServerStatDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<GameServerStatDto>>(collection)));
    }
}
