using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakePlaytimeApi : IPlaytimeApi
{
    public Task<ApiResult<PlaytimeObservationBatchResultDto>> BatchObservations(
        PlaytimeObservationBatchDto dto,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var items = dto.Observations.Select(x => new PlaytimeObservationItemResultDto
        {
            ObservationId = x.ObservationId,
            Outcome = PlaytimeObservationOutcome.Accepted
        }).ToList();
        return Task.FromResult(new ApiResult<PlaytimeObservationBatchResultDto>(
            HttpStatusCode.OK,
            new ApiResponse<PlaytimeObservationBatchResultDto>(
                new PlaytimeObservationBatchResultDto { Items = items })));
    }

    public Task<ApiResult<IReadOnlyList<PlayerSessionDto>>> GetPlayerSessions(
        Guid playerId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skipEntries,
        int takeEntries,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ApiResult<IReadOnlyList<PlayerSessionDto>>(
            HttpStatusCode.OK,
            new ApiResponse<IReadOnlyList<PlayerSessionDto>>(Array.Empty<PlayerSessionDto>())));
    }

    public Task<ApiResult<PlaytimeTotalsDto>> GetTotals(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? playerId,
        Guid? serverId,
        GameType? gameType,
        string? mapName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ApiResult<PlaytimeTotalsDto>(
            HttpStatusCode.OK,
            new ApiResponse<PlaytimeTotalsDto>(new PlaytimeTotalsDto
            {
                MeasuredSeconds = 0,
                UnknownMapSeconds = 0,
                DataThroughUtc = DateTime.UnixEpoch,
                PlayerId = playerId,
                ServerId = serverId,
                GameType = gameType,
                MapName = mapName
            })));
    }
}
