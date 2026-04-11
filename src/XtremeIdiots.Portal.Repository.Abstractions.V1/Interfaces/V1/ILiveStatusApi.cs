using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface ILiveStatusApi
{
    Task<ApiResult> SetGameServerLiveStatus(Guid gameServerId, SetGameServerLiveStatusDto dto, CancellationToken cancellationToken = default);
    Task<ApiResult<GameServerLiveStatusDto>> GetGameServerLiveStatus(Guid gameServerId, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<GameServerLiveStatusDto>>> GetAllGameServerLiveStatuses(CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<LivePlayerDto>>> GetGameServerLivePlayers(Guid gameServerId, CancellationToken cancellationToken = default);
}
