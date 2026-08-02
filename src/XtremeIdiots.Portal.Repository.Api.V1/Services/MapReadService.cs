using System.Net;

using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Default (uncached) implementation of <see cref="IMapReadService"/>. Behaviour mirrors
    /// the pre-refactor <c>MapsController.GetMap</c> paths for both overloads.
    /// </summary>
    public sealed class MapReadService : IMapReadService
    {
        private readonly PortalDbContext context;

        public MapReadService(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        public async Task<ApiResult<MapDto>> GetMapByIdAsync(Guid mapId, CancellationToken cancellationToken)
        {
            var map = await context.Maps
                .Include(m => m.MapVotes)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MapId == mapId, cancellationToken)
                .ConfigureAwait(false);

            if (map == null)
            {
                return new ApiResult<MapDto>(HttpStatusCode.NotFound);
            }

            return new ApiResponse<MapDto>(map.ToDto()).ToApiResult();
        }

        public async Task<ApiResult<MapDto>> GetMapByGameTypeAndNameAsync(GameType gameType, string mapName, CancellationToken cancellationToken)
        {
            var map = await context.Maps
                .Include(m => m.MapVotes)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.GameType == gameType.ToGameTypeInt() && m.MapName == mapName, cancellationToken)
                .ConfigureAwait(false);

            if (map == null)
            {
                return new ApiResult<MapDto>(HttpStatusCode.NotFound);
            }

            return new ApiResponse<MapDto>(map.ToDto()).ToApiResult();
        }
    }
}
