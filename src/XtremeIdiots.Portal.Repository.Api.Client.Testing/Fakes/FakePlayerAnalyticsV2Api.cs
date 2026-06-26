using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakePlayerAnalyticsV2Api : IPlayerAnalyticsV2Api
{
    private PlayerOverviewDto _overview = new();
    private PlayerTrendsDto _trends = new();
    private PlayerRelatedActivityDto _relatedActivity = new();
    private PlayerModerationSummaryDto _moderationSummary = new();

    public FakePlayerAnalyticsV2Api SetOverview(PlayerOverviewDto overview) { _overview = overview; return this; }
    public FakePlayerAnalyticsV2Api SetTrends(PlayerTrendsDto trends) { _trends = trends; return this; }
    public FakePlayerAnalyticsV2Api SetRelatedActivity(PlayerRelatedActivityDto relatedActivity) { _relatedActivity = relatedActivity; return this; }
    public FakePlayerAnalyticsV2Api SetModerationSummary(PlayerModerationSummaryDto moderationSummary) { _moderationSummary = moderationSummary; return this; }

    public FakePlayerAnalyticsV2Api Reset()
    {
        _overview = new();
        _trends = new();
        _relatedActivity = new();
        _moderationSummary = new();
        return this;
    }

    public Task<ApiResult<PlayerOverviewDto>> GetOverview(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayerOverviewDto>(HttpStatusCode.OK, new ApiResponse<PlayerOverviewDto>(_overview)));
    }

    public Task<ApiResult<PlayerTrendsDto>> GetTrends(Guid playerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayerTrendsDto>(HttpStatusCode.OK, new ApiResponse<PlayerTrendsDto>(_trends)));
    }

    public Task<ApiResult<PlayerRelatedActivityDto>> GetRelatedActivity(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayerRelatedActivityDto>(HttpStatusCode.OK, new ApiResponse<PlayerRelatedActivityDto>(_relatedActivity)));
    }

    public Task<ApiResult<PlayerModerationSummaryDto>> GetModerationSummary(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayerModerationSummaryDto>(HttpStatusCode.OK, new ApiResponse<PlayerModerationSummaryDto>(_moderationSummary)));
    }
}
