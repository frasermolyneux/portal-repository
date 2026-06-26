using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Games;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IGameAnalyticsApi
{
    Task<ApiResult<GameOverviewDto>> GetOverview(GameType gameType, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<ApiResult<GameTimeseriesDto>> GetTimeseries(GameType gameType, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default);
    Task<ApiResult<GameServerBreakdownDto>> GetServerBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);
    Task<ApiResult<GamePlayerBreakdownDto>> GetPlayerBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);
    Task<ApiResult<GameMapBreakdownDto>> GetMapBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);
}