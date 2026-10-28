using System.Net;

using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Uncached map reads. COD4 and COD4x share the COD4 catalogue.
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
            var map = await CatalogueQuery(context)
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
            var mapGameType = (int)gameType.GetMapGameType();
            var map = await CatalogueQuery(context)
                .Where(m => m.GameType == mapGameType && m.MapName == mapName)
                .OrderBy(m => m.MapId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (map == null)
            {
                return new ApiResult<MapDto>(HttpStatusCode.NotFound);
            }

            return new ApiResponse<MapDto>(map.ToDto()).ToApiResult();
        }

        // Keep ratings complete while old consumers drain before the separate data cutover.
        internal static IQueryable<Map> CatalogueQuery(PortalDbContext context) =>
            from map in context.Maps.AsNoTracking()
            join legacy in context.Maps.Where(m => m.GameType == (int)GameType.CallOfDuty4x)
                on new { map.GameType, map.MapName }
                equals new { GameType = (int)GameType.CallOfDuty4, legacy.MapName } into legacyMaps
            from legacy in legacyMaps.DefaultIfEmpty()
            select new Map
            {
                MapId = map.MapId,
                GameType = map.GameType,
                MapName = map.MapName,
                DisplayName = map.DisplayName,
                MapImageUri = map.MapImageUri,
                MapFiles = map.MapFiles,
                MapStatus = map.MapStatus,
                TotalLikes = map.TotalLikes + (legacy == null ? 0 : legacy.TotalLikes),
                TotalDislikes = map.TotalDislikes + (legacy == null ? 0 : legacy.TotalDislikes),
                TotalVotes = map.TotalVotes + (legacy == null ? 0 : legacy.TotalVotes),
                LikePercentage = legacy == null ? map.LikePercentage
                    : map.TotalVotes + legacy.TotalVotes == 0 ? 0
                    : (map.TotalLikes + legacy.TotalLikes) * 100.0 / (map.TotalVotes + legacy.TotalVotes),
                DislikePercentage = legacy == null ? map.DislikePercentage
                    : map.TotalVotes + legacy.TotalVotes == 0 ? 0
                    : (map.TotalDislikes + legacy.TotalDislikes) * 100.0 / (map.TotalVotes + legacy.TotalVotes)
            };
    }
}
