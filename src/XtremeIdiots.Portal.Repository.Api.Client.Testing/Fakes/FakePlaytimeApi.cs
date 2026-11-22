using System.Net;
using System.Collections.Concurrent;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakePlaytimeApi : IPlaytimeApi
{
    private readonly ConcurrentDictionary<(Guid ServerId, Guid PlayerId), CurrentPlayerSessionResultDto> currentSessions = new();

    public FakePlaytimeApi SetCurrentSession(Guid serverId, CurrentPlayerSessionResultDto result)
    {
        currentSessions[(serverId, result.PlayerId)] = result;
        return this;
    }

    public FakePlaytimeApi Reset()
    {
        currentSessions.Clear();
        return this;
    }

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

    public Task<ApiResult<IReadOnlyList<CurrentPlayerSessionResultDto>>> GetCurrentSessions(
        Guid serverId,
        CurrentPlayerSessionsRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (serverId == Guid.Empty || dto?.PlayerIds is null || dto.PlayerIds.Count is < 1 or > 100 ||
            dto.PlayerIds.Any(x => x == Guid.Empty) || dto.PlayerIds.Distinct().Count() != dto.PlayerIds.Count)
        {
            return Task.FromResult(new ApiResult<IReadOnlyList<CurrentPlayerSessionResultDto>>(HttpStatusCode.BadRequest));
        }

        IReadOnlyList<CurrentPlayerSessionResultDto> results = dto.PlayerIds
            .Select(id => currentSessions.TryGetValue((serverId, id), out var result)
                ? result
                : new CurrentPlayerSessionResultDto { PlayerId = id, Status = CurrentPlayerSessionStatus.Unavailable })
            .ToList();
        return Task.FromResult(new ApiResult<IReadOnlyList<CurrentPlayerSessionResultDto>>(
            HttpStatusCode.OK, new ApiResponse<IReadOnlyList<CurrentPlayerSessionResultDto>>(results)));
    }
}
