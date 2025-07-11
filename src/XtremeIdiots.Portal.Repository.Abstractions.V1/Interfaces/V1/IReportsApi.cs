using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Reports;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IReportsApi
    {
        Task<ApiResult<ReportDto>> GetReport(Guid reportId, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<ReportDto>>> GetReports(GameType? gameType, Guid? gameServerId, DateTime? cutoff, ReportsFilter? filter, int skipEntries, int takeEntries, ReportsOrder? order, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateReports(List<CreateReportDto> createReportDtos, CancellationToken cancellationToken = default);

        Task<ApiResult> CloseReport(Guid reportId, CloseReportDto closeReportDto, CancellationToken cancellationToken = default);
    }
}