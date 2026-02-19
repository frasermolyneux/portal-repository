using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakePlayerAnalyticsApi : IPlayerAnalyticsApi
{
    private List<PlayerAnalyticEntryDto> _cumulativeDailyPlayers = [];
    private List<PlayerAnalyticPerGameEntryDto> _newDailyPlayersPerGame = [];
    private List<PlayerAnalyticPerGameEntryDto> _playersDropOffPerGame = [];

    public FakePlayerAnalyticsApi SetCumulativeDailyPlayers(List<PlayerAnalyticEntryDto> entries) { _cumulativeDailyPlayers = entries; return this; }
    public FakePlayerAnalyticsApi SetNewDailyPlayersPerGame(List<PlayerAnalyticPerGameEntryDto> entries) { _newDailyPlayersPerGame = entries; return this; }
    public FakePlayerAnalyticsApi SetPlayersDropOffPerGame(List<PlayerAnalyticPerGameEntryDto> entries) { _playersDropOffPerGame = entries; return this; }
    public FakePlayerAnalyticsApi Reset() { _cumulativeDailyPlayers = []; _newDailyPlayersPerGame = []; _playersDropOffPerGame = []; return this; }

    public Task<ApiResult<CollectionModel<PlayerAnalyticEntryDto>>> GetCumulativeDailyPlayers(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        var collection = new CollectionModel<PlayerAnalyticEntryDto> { Items = _cumulativeDailyPlayers };
        return Task.FromResult(new ApiResult<CollectionModel<PlayerAnalyticEntryDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerAnalyticEntryDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>> GetNewDailyPlayersPerGame(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        var collection = new CollectionModel<PlayerAnalyticPerGameEntryDto> { Items = _newDailyPlayersPerGame };
        return Task.FromResult(new ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerAnalyticPerGameEntryDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>> GetPlayersDropOffPerGameJson(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        var collection = new CollectionModel<PlayerAnalyticPerGameEntryDto> { Items = _playersDropOffPerGame };
        return Task.FromResult(new ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerAnalyticPerGameEntryDto>>(collection)));
    }
}
