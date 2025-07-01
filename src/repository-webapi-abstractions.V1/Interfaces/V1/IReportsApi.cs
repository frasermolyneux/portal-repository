using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Reports;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1
{
    public interface IReportsApi
    {
        Task<ApiResponseDto<ReportDto>> GetReport(Guid reportId);
        Task<ApiResponseDto<ReportsCollectionDto>> GetReports(GameType? gameType, Guid? gameServerId, DateTime? cutoff, ReportsFilter? filter, int skipEntries, int takeEntries, ReportsOrder? order);

        Task<ApiResponseDto> CreateReports(List<CreateReportDto> createReportDtos);

        Task<ApiResponseDto> CloseReport(Guid reportId, CloseReportDto closeReportDto);
    }
}