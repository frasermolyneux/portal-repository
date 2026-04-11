using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Api.V1.TableStorage;

public interface ILiveStatusStore
{
    Task SetServerLiveStatusAsync(Guid serverId, GameServerLiveStatusEntity entity, CancellationToken cancellationToken = default);
    Task<GameServerLiveStatusDto?> GetServerLiveStatusAsync(Guid serverId, CancellationToken cancellationToken = default);
    Task<List<GameServerLiveStatusDto>> GetAllServerLiveStatusesAsync(CancellationToken cancellationToken = default);
    Task SetLivePlayersAsync(Guid serverId, List<GameServerLivePlayerEntity> players, CancellationToken cancellationToken = default);
    Task<List<LivePlayerDto>> GetLivePlayersAsync(Guid serverId, CancellationToken cancellationToken = default);
}
