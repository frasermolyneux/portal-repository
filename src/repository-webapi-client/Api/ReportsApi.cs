using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Reports;

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class ReportsApi : BaseApi, IReportsApi
    {
        public ReportsApi(ILogger<ReportsApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<ReportDto>> GetReport(Guid reportId)
        {
            var request = await CreateRequestAsync($"reports/{reportId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<ReportDto>();
        }

        public async Task<ApiResponseDto<ReportsCollectionDto>> GetReports(GameType? gameType, Guid? gameServerId, DateTime? cutoff, ReportsFilter? filter, int skipEntries, int takeEntries, ReportsOrder? order)
        {
            var request = await CreateRequestAsync("reports", Method.Get);

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

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<ReportsCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateReports(List<CreateReportDto> createReportDtos)
        {
            var request = await CreateRequestAsync("reports", Method.Post);
            request.AddJsonBody(createReportDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CloseReport(Guid reportId, CloseReportDto closeReportDto)
        {
            var request = await CreateRequestAsync($"reports/{reportId}/close", Method.Post);
            request.AddJsonBody(closeReportDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
