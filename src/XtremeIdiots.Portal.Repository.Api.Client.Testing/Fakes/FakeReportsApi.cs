using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Reports;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeReportsApi : IReportsApi
{
    private readonly ConcurrentDictionary<Guid, ReportDto> _reports = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);

    public FakeReportsApi AddReport(ReportDto report) { _reports[report.ReportId] = report; return this; }
    public FakeReportsApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeReportsApi Reset() { _reports.Clear(); _errorResponses.Clear(); return this; }

    public Task<ApiResult<ReportDto>> GetReport(Guid reportId, CancellationToken cancellationToken = default)
    {
        if (_reports.TryGetValue(reportId, out var report))
            return Task.FromResult(new ApiResult<ReportDto>(HttpStatusCode.OK, new ApiResponse<ReportDto>(report)));
        return Task.FromResult(new ApiResult<ReportDto>(HttpStatusCode.NotFound, new ApiResponse<ReportDto>(new ApiError("NOT_FOUND", "Report not found"))));
    }

    public Task<ApiResult<CollectionModel<ReportDto>>> GetReports(GameType? gameType, Guid? gameServerId, DateTime? cutoff, ReportsFilter? filter, int skipEntries, int takeEntries, ReportsOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _reports.Values.AsEnumerable();
        if (gameType.HasValue) items = items.Where(r => r.GameType == gameType.Value);
        if (gameServerId.HasValue) items = items.Where(r => r.GameServerId == gameServerId.Value);
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<ReportDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<ReportDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<ReportDto>>(collection)));
    }

    public Task<ApiResult> CreateReports(List<CreateReportDto> createReportDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CloseReport(Guid reportId, CloseReportDto closeReportDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
