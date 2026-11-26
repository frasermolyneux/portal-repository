using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IPlayerAnalyticsV2Api
{
    Task<ApiResult<PlayersOverviewDto>> GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<ApiResult<PlayersTimeseriesDto>> GetTimeseries(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default);

    Task<ApiResult<PlayersTimeseriesDto>> GetTimeseries(
        DateTime fromUtc,
        DateTime toUtc,
        AnalyticsBucket bucket,
        AnalyticsCompareMode compareMode,
        int comparePeriods = AnalyticsQueryDefaults.DefaultComparePeriods,
        AnalyticsAlignMode alignMode = AnalyticsAlignMode.None,
        string timezone = "UTC",
        bool normalize = false,
        CancellationToken cancellationToken = default);

    Task<ApiResult<PlayersTopDto>> GetTop(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);

    Task<ApiResult<PlayersByGameDto>> GetByGame(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<ApiResult<PlayersByServerDto>> GetByServer(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);

    Task<ApiResult<PlayerDetailDto>> GetPlayerDetail(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>Gets recorded activity for the supplied UTC window and caller-authorized servers.</summary>
    /// <remarks>An empty server scope is never treated as unrestricted. Request chat only when the user can read the player's game chat log.</remarks>
    Task<ApiResult<PlayerActivityDto>> GetPlayerActivity(Guid playerId, DateTime fromUtc, DateTime toUtc,
        IReadOnlyCollection<Guid> serverIds, bool includeChat = false, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This player analytics API implementation does not support player activity.");

    Task<ApiResult<PlayerTrendsDto>> GetPlayerTimeseries(Guid playerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default);

    Task<ApiResult<PlayerTrendsDto>> GetPlayerTimeseries(
        Guid playerId,
        DateTime fromUtc,
        DateTime toUtc,
        AnalyticsBucket bucket,
        AnalyticsCompareMode compareMode,
        int comparePeriods = AnalyticsQueryDefaults.DefaultComparePeriods,
        AnalyticsAlignMode alignMode = AnalyticsAlignMode.None,
        string timezone = "UTC",
        bool normalize = false,
        CancellationToken cancellationToken = default);
}
