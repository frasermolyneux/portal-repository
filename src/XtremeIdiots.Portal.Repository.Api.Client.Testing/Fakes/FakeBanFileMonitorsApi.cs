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

    public Task<ApiResult<BanFileMonitorDto>> UpsertBanFileMonitorStatus(UpsertBanFileMonitorStatusDto upsertDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsertDto);

        // Find an existing monitor for the game server, or create one keyed by a new GUID.
        var existing = _monitors.Values.FirstOrDefault(m => m.GameServerId == upsertDto.GameServerId);
        var created = existing is null;
        existing ??= new BanFileMonitorDto { BanFileMonitorId = Guid.NewGuid(), GameServerId = upsertDto.GameServerId };

        var updated = existing with
        {
            LastCheckUtc = upsertDto.LastCheckUtc ?? existing.LastCheckUtc,
            LastCheckResult = upsertDto.LastCheckResult ?? existing.LastCheckResult,
            LastCheckErrorMessage = upsertDto.LastCheckErrorMessage ?? existing.LastCheckErrorMessage,
            RemoteFilePath = upsertDto.RemoteFilePath ?? existing.RemoteFilePath,
            ResolvedForMod = upsertDto.ResolvedForMod ?? existing.ResolvedForMod,
            RemoteFileSize = upsertDto.RemoteFileSize ?? existing.RemoteFileSize,
            LastImportUtc = upsertDto.LastImportUtc ?? existing.LastImportUtc,
            LastImportBanCount = upsertDto.LastImportBanCount ?? existing.LastImportBanCount,
            LastImportSampleNames = upsertDto.LastImportSampleNames ?? existing.LastImportSampleNames,
            LastPushUtc = upsertDto.LastPushUtc ?? existing.LastPushUtc,
            LastPushedETag = upsertDto.LastPushedETag ?? existing.LastPushedETag,
            LastPushedSize = upsertDto.LastPushedSize ?? existing.LastPushedSize,
            LastCentralBlobETag = upsertDto.LastCentralBlobETag ?? existing.LastCentralBlobETag,
            LastCentralBlobUtc = upsertDto.LastCentralBlobUtc ?? existing.LastCentralBlobUtc,
            ConsecutiveFailureCount = upsertDto.ConsecutiveFailureCount ?? existing.ConsecutiveFailureCount,
            RemoteTotalLineCount = upsertDto.RemoteTotalLineCount ?? existing.RemoteTotalLineCount,
            RemoteUntaggedCount = upsertDto.RemoteUntaggedCount ?? existing.RemoteUntaggedCount,
            RemoteBanSyncCount = upsertDto.RemoteBanSyncCount ?? existing.RemoteBanSyncCount,
            RemoteExternalCount = upsertDto.RemoteExternalCount ?? existing.RemoteExternalCount
        };

        _monitors[updated.BanFileMonitorId] = updated;

        return Task.FromResult(new ApiResult<BanFileMonitorDto>(
            created ? HttpStatusCode.Created : HttpStatusCode.OK,
            new ApiResponse<BanFileMonitorDto>(updated)));
    }
}
