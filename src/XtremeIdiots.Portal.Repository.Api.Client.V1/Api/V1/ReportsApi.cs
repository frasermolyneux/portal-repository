using Microsoft.Extensions.Logging;



using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Reports;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class ReportsApi : BaseApi<RepositoryApiClientOptions>, IReportsApi
    {
        public ReportsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<ReportDto>> GetReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/reports/{reportId}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<ReportDto>();
        }

        public async Task<ApiResult<CollectionModel<ReportDto>>> GetReports(GameType? gameType, Guid? gameServerId, DateTime? cutoff, ReportsFilter? filter, int skipEntries, int takeEntries, ReportsOrder? order, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/reports", Method.Get).ConfigureAwait(false);

            if (gameType.HasValue)
                request.AddQueryParameter("gameType", gameType.ToString());

            if (gameServerId.HasValue)
                request.AddQueryParameter("gameServerId", gameServerId.ToString());

            if (cutoff.HasValue)
                request.AddQueryParameter("cutoff", cutoff.Value.ToString("MM/dd/yyyy HH:mm:ss"));

            if (filter.HasValue)
                request.AddQueryParameter("filter", filter.ToString());

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<ReportDto>>();
        }

        public async Task<ApiResult> CreateReports(List<CreateReportDto> createReportDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/reports", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(createReportDtos);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> CloseReport(Guid reportId, CloseReportDto closeReportDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/reports/{reportId}/close", Method.Patch).ConfigureAwait(false);
            request.AddJsonBody(closeReportDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}


