using System.Collections.Concurrent;
using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.CentralBanFileStatus;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeCentralBanFileStatusApi : ICentralBanFileStatusApi
{
    private readonly ConcurrentDictionary<GameType, CentralBanFileStatusDto> _statuses = new();

    public FakeCentralBanFileStatusApi AddStatus(CentralBanFileStatusDto status)
    {
        _statuses[status.GameType] = status;
        return this;
    }

    public FakeCentralBanFileStatusApi Reset()
    {
        _statuses.Clear();
        return this;
    }

    public Task<ApiResult<CentralBanFileStatusDto>> GetCentralBanFileStatus(GameType gameType, CancellationToken cancellationToken = default)
    {
        if (_statuses.TryGetValue(gameType, out var status))
            return Task.FromResult(new ApiResult<CentralBanFileStatusDto>(HttpStatusCode.OK, new ApiResponse<CentralBanFileStatusDto>(status)));

        return Task.FromResult(new ApiResult<CentralBanFileStatusDto>(HttpStatusCode.NotFound, new ApiResponse<CentralBanFileStatusDto>(new ApiError("NOT_FOUND", "Central ban file status not found"))));
    }

    public Task<ApiResult<CollectionModel<CentralBanFileStatusDto>>> GetCentralBanFileStatuses(CancellationToken cancellationToken = default)
    {
        var items = _statuses.Values.OrderBy(s => s.GameType).ToList();
        return Task.FromResult(new ApiResult<CollectionModel<CentralBanFileStatusDto>>(
            HttpStatusCode.OK,
            new ApiResponse<CollectionModel<CentralBanFileStatusDto>>(new CollectionModel<CentralBanFileStatusDto> { Items = items })));
    }

    public Task<ApiResult<CentralBanFileStatusDto>> UpsertCentralBanFileStatus(UpsertCentralBanFileStatusDto upsertDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upsertDto);

        var created = !_statuses.ContainsKey(upsertDto.GameType);
        var existing = _statuses.GetValueOrDefault(upsertDto.GameType) ?? new CentralBanFileStatusDto { GameType = upsertDto.GameType };

        var updated = existing with
        {
            BlobLastRegeneratedUtc = upsertDto.BlobLastRegeneratedUtc ?? existing.BlobLastRegeneratedUtc,
            BlobETag = upsertDto.BlobETag ?? existing.BlobETag,
            BlobSizeBytes = upsertDto.BlobSizeBytes ?? existing.BlobSizeBytes,
            TotalLineCount = upsertDto.TotalLineCount ?? existing.TotalLineCount,
            BanSyncLineCount = upsertDto.BanSyncLineCount ?? existing.BanSyncLineCount,
            ExternalLineCount = upsertDto.ExternalLineCount ?? existing.ExternalLineCount,
            ExternalSourceLastModifiedUtc = upsertDto.ExternalSourceLastModifiedUtc ?? existing.ExternalSourceLastModifiedUtc,
            LastRegenerationDurationMs = upsertDto.LastRegenerationDurationMs ?? existing.LastRegenerationDurationMs,
            LastRegenerationError = upsertDto.LastRegenerationError ?? existing.LastRegenerationError,
            ActiveBanSetHash = upsertDto.ActiveBanSetHash ?? existing.ActiveBanSetHash,
            LastUpdatedUtc = DateTime.UtcNow
        };

        _statuses[upsertDto.GameType] = updated;

        return Task.FromResult(new ApiResult<CentralBanFileStatusDto>(
            created ? HttpStatusCode.Created : HttpStatusCode.OK,
            new ApiResponse<CentralBanFileStatusDto>(updated)));
    }
}
