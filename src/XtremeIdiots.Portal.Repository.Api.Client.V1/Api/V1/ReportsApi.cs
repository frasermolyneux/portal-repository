using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Reports;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class ReportsApi : BaseApi, IReportsApi
    {
        public ReportsApi(ILogger<ReportsApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<ReportDto>> GetReport(Guid reportId)
        {
            var request = await CreateRequestAsync($"v1/reports/{reportId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<ReportDto>();
        }

        public async Task<ApiResponseDto<ReportsCollectionDto>> GetReports(GameType? gameType, Guid? gameServerId, DateTime? cutoff, ReportsFilter? filter, int skipEntries, int takeEntries, ReportsOrder? order)
        {
            var request = await CreateRequestAsync("v1/reports", Method.Get);

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
            var request = await CreateRequestAsync("v1/reports", Method.Post);
            request.AddJsonBody(createReportDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CloseReport(Guid reportId, CloseReportDto closeReportDto)
        {
            var request = await CreateRequestAsync($"v1/reports/{reportId}/close", Method.Post);
            request.AddJsonBody(closeReportDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
