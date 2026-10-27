using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IGameServersEventsApi
{
    Task<ApiResult<HistoryPageDto<GameServerEventDto>>> GetGameServerEventsPage(GameType? gameType = null, Guid? gameServerId = null, string? eventType = null, int skipEntries = 0, int takeEntries = 20, GameServerEventOrder? order = null, DateTime? fromUtc = null, CancellationToken cancellationToken = default);

    Task<ApiResult<CollectionModel<GameServerEventDto>>> GetGameServerEvents(GameType? gameType = null, Guid? gameServerId = null, string? eventType = null, int skipEntries = 0, int takeEntries = 20, GameServerEventOrder? order = null, CancellationToken cancellationToken = default);

    Task<ApiResult> CreateGameServerEvent(CreateGameServerEventDto createGameServerEventDto, CancellationToken cancellationToken = default);
    Task<ApiResult> CreateGameServerEvents(List<CreateGameServerEventDto> createGameServerEventDtos, CancellationToken cancellationToken = default);
}
