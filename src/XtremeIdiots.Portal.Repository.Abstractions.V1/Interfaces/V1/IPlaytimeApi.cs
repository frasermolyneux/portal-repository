using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IPlaytimeApi
{
    Task<ApiResult<PlaytimeObservationBatchResultDto>> BatchObservations(
        PlaytimeObservationBatchDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<PlayerSessionDto>>> GetPlayerSessions(
        Guid playerId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skipEntries,
        int takeEntries,
        CancellationToken cancellationToken = default);

    Task<ApiResult<PlaytimeTotalsDto>> GetTotals(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? playerId,
        Guid? serverId,
        GameType? gameType,
        string? mapName,
        CancellationToken cancellationToken = default);
}
