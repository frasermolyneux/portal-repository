using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Repository-side seam for reading game server rows. Extracted from the controller
    /// so caching can be layered as a decorator without controller-level cache hacks.
    /// </summary>
    public interface IGameServerReadService
    {
        Task<ApiResult<GameServerDto>> GetGameServerAsync(Guid gameServerId, CancellationToken cancellationToken);
    }
}
