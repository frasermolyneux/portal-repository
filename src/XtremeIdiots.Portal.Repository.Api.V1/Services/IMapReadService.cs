using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Repository-side seam for reading single map records. Extracted so the two GetMap
    /// overloads can be layered with a caching decorator; mutation-side controllers still
    /// write directly to <see cref="DataLib.PortalDbContext"/> and evict via
    /// <see cref="Caching.IRepositoryCacheInvalidator.InvalidateMapAsync"/>.
    /// </summary>
    public interface IMapReadService
    {
        /// <summary>Returns a single map by its unique identifier, or <c>NotFound</c>.</summary>
        Task<ApiResult<MapDto>> GetMapByIdAsync(Guid mapId, CancellationToken cancellationToken);

        /// <summary>Returns a single map by game type and map name, or <c>NotFound</c>.</summary>
        Task<ApiResult<MapDto>> GetMapByGameTypeAndNameAsync(GameType gameType, string mapName, CancellationToken cancellationToken);
    }
}
