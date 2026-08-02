using System.Net;

using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Default (uncached) implementation of <see cref="IGameServerReadService"/> — hits SQL directly.
    /// Behaviour mirrors the pre-refactor <c>GameServersController.GetGameServer</c> path so wrapping
    /// in a caching decorator is transparent to callers.
    /// </summary>
    public sealed class GameServerReadService : IGameServerReadService
    {
        private readonly PortalDbContext context;

        public GameServerReadService(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        public async Task<ApiResult<GameServerDto>> GetGameServerAsync(Guid gameServerId, CancellationToken cancellationToken)
        {
            var gameServer = await context.GameServers
                .Include(gs => gs.BanFileMonitors)
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken)
                .ConfigureAwait(false);

            if (gameServer == null)
            {
                return new ApiResult<GameServerDto>(HttpStatusCode.NotFound);
            }

            return new ApiResponse<GameServerDto>(gameServer.ToDto()).ToApiResult();
        }
    }
}
