using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RecentPlayers;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeRecentPlayersApi : IRecentPlayersApi
{
    private readonly ConcurrentBag<RecentPlayerDto> _recentPlayers = [];

    public FakeRecentPlayersApi AddRecentPlayer(RecentPlayerDto recentPlayer) { _recentPlayers.Add(recentPlayer); return this; }
    public FakeRecentPlayersApi Reset() { while (_recentPlayers.TryTake(out _)) { } return this; }

    public Task<ApiResult<CollectionModel<RecentPlayerDto>>> GetRecentPlayers(GameType? gameType, Guid? gameServerId, DateTime? cutoff, RecentPlayersFilter? filter, int skipEntries, int takeEntries, RecentPlayersOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _recentPlayers.AsEnumerable();
        if (gameType.HasValue) items = items.Where(r => r.GameType == gameType.Value);
        if (gameServerId.HasValue) items = items.Where(r => r.GameServerId == gameServerId.Value);
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<RecentPlayerDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<RecentPlayerDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<RecentPlayerDto>>(collection)));
    }

    public Task<ApiResult> CreateRecentPlayers(List<CreateRecentPlayerDto> createRecentPlayerDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
