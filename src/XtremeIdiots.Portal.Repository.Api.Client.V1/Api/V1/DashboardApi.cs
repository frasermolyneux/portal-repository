using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public class DashboardApi : BaseApi<RepositoryApiClientOptions>, IDashboardApi
{
    public DashboardApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
        : base(logger, apiTokenProvider, restClientService, options)
    {
    }

    public async Task<ApiResult<DashboardSummaryDto>> GetDashboardSummary(CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/dashboard/summary", Method.Get).ConfigureAwait(false);

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<DashboardSummaryDto>();
    }

    public async Task<ApiResult<CollectionModel<AdminLeaderboardEntryDto>>> GetAdminLeaderboard(int days, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/dashboard/admin-leaderboard", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("days", days.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<CollectionModel<AdminLeaderboardEntryDto>>();
    }

    public async Task<ApiResult<CollectionModel<ModerationTrendDataPointDto>>> GetModerationTrend(int days, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/dashboard/moderation-trend", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("days", days.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<CollectionModel<ModerationTrendDataPointDto>>();
    }

    public async Task<ApiResult<ServerUtilizationCollectionDto>> GetServerUtilization(CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/dashboard/server-utilization", Method.Get).ConfigureAwait(false);

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<ServerUtilizationCollectionDto>();
    }
}
