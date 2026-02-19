using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeBanFileMonitorsApi : IBanFileMonitorsApi
{
    private readonly ConcurrentDictionary<Guid, BanFileMonitorDto> _monitors = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);

    public FakeBanFileMonitorsApi AddBanFileMonitor(BanFileMonitorDto monitor) { _monitors[monitor.BanFileMonitorId] = monitor; return this; }
    public FakeBanFileMonitorsApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeBanFileMonitorsApi Reset() { _monitors.Clear(); _errorResponses.Clear(); return this; }

    public Task<ApiResult<BanFileMonitorDto>> GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default)
    {
        if (_monitors.TryGetValue(banFileMonitorId, out var m))
            return Task.FromResult(new ApiResult<BanFileMonitorDto>(HttpStatusCode.OK, new ApiResponse<BanFileMonitorDto>(m)));
        return Task.FromResult(new ApiResult<BanFileMonitorDto>(HttpStatusCode.NotFound, new ApiResponse<BanFileMonitorDto>(new ApiError("NOT_FOUND", "Ban file monitor not found"))));
    }

    public Task<ApiResult<CollectionModel<BanFileMonitorDto>>> GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _monitors.Values.AsEnumerable();
        if (gameServerId.HasValue) items = items.Where(m => m.GameServerId == gameServerId.Value);
        if (banFileMonitorIds != null) items = items.Where(m => banFileMonitorIds.Contains(m.BanFileMonitorId));
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<BanFileMonitorDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<BanFileMonitorDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<BanFileMonitorDto>>(collection)));
    }

    public Task<ApiResult> CreateBanFileMonitor(CreateBanFileMonitorDto createBanFileMonitorDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateBanFileMonitor(EditBanFileMonitorDto editBanFileMonitorDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
