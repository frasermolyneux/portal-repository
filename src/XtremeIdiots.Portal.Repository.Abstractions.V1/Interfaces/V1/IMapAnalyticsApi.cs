using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IMapAnalyticsApi
{
    Task<ApiResult<MapInsightsDto>> GetInsights(DateTime fromUtc, DateTime toUtc, GameType? gameType = null, Guid? gameServerId = null, CancellationToken cancellationToken = default);
    Task<ApiResult<MapInsightsDto>> GetVoteInsights(DateTime fromUtc, DateTime toUtc, GameType? gameType = null, Guid? gameServerId = null, CancellationToken cancellationToken = default);

    Task<ApiResult<MapsOverviewDto>> GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<ApiResult<MapsHotspotsDto>> GetHotspots(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);

    Task<ApiResult<MapsTopPlayedDto>> GetTopPlayed(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);

    Task<ApiResult<MapsTopVotedDto>> GetTopVoted(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);

    Task<ApiResult<MapsByGameDto>> GetByGame(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<ApiResult<MapsByServerDto>> GetByServer(Guid gameServerId, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);

    Task<ApiResult<MapDetailDto>> GetMapDetail(Guid mapId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
