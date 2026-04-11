using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Api.V1.TableStorage;

/// <summary>
/// No-op implementation used when Table Storage is not configured.
/// All reads return empty/null; all writes are silently ignored.
/// </summary>
public class NullLiveStatusStore : ILiveStatusStore
{
    public Task SetServerLiveStatusAsync(Guid serverId, GameServerLiveStatusEntity entity, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<GameServerLiveStatusDto?> GetServerLiveStatusAsync(Guid serverId, CancellationToken cancellationToken = default)
        => Task.FromResult<GameServerLiveStatusDto?>(null);

    public Task<List<GameServerLiveStatusDto>> GetAllServerLiveStatusesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new List<GameServerLiveStatusDto>());

    public Task SetLivePlayersAsync(Guid serverId, List<GameServerLivePlayerEntity> players, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<List<LivePlayerDto>> GetLivePlayersAsync(Guid serverId, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<LivePlayerDto>());
}
