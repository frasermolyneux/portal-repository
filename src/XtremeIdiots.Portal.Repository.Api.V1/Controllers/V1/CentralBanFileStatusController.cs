using System.Net;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.CentralBanFileStatus;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.DataLib;
using XiCentralBanFileStatusEntity = XtremeIdiots.Portal.Repository.DataLib.CentralBanFileStatus;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    /// <summary>
    /// API controller for the per-game-type central ban file status row maintained by portal-sync.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("v{version:apiVersion}")]
    public class CentralBanFileStatusController : ControllerBase, ICentralBanFileStatusApi
    {
        private readonly PortalDbContext context;

        public CentralBanFileStatusController(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        [HttpGet("central-ban-file-status/{gameType}")]
        [ProducesResponseType<CentralBanFileStatusDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCentralBanFileStatus(GameType gameType, CancellationToken cancellationToken = default)
        {
            var response = await ((ICentralBanFileStatusApi)this).GetCentralBanFileStatus(gameType, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CentralBanFileStatusDto>> ICentralBanFileStatusApi.GetCentralBanFileStatus(GameType gameType, CancellationToken cancellationToken)
        {
            var gtInt = (int)gameType;
            var entity = await context.CentralBanFileStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.GameType == gtInt, cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
                return new ApiResult<CentralBanFileStatusDto>(HttpStatusCode.NotFound);

            return new ApiResponse<CentralBanFileStatusDto>(entity.ToDto()).ToApiResult();
        }

        [HttpGet("central-ban-file-status")]
        [ProducesResponseType<CollectionModel<CentralBanFileStatusDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCentralBanFileStatuses(CancellationToken cancellationToken = default)
        {
            var response = await ((ICentralBanFileStatusApi)this).GetCentralBanFileStatuses(cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<CentralBanFileStatusDto>>> ICentralBanFileStatusApi.GetCentralBanFileStatuses(CancellationToken cancellationToken)
        {
            var entities = await context.CentralBanFileStatuses
                .AsNoTracking()
                .OrderBy(s => s.GameType)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var items = entities.Select(e => e.ToDto()).ToList();
            var data = new CollectionModel<CentralBanFileStatusDto>(items);
            return new ApiResponse<CollectionModel<CentralBanFileStatusDto>>(data)
            {
                Pagination = new ApiPagination(items.Count, items.Count, 0, items.Count)
            }.ToApiResult();
        }

        [HttpPut("central-ban-file-status/{gameType}")]
        [ProducesResponseType<CentralBanFileStatusDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<CentralBanFileStatusDto>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpsertCentralBanFileStatus(
            GameType gameType,
            [FromBody] UpsertCentralBanFileStatusDto upsertDto,
            CancellationToken cancellationToken = default)
        {
            if (upsertDto is null)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))).ToHttpResult();

            if (upsertDto.GameType != gameType)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.EntityIdMismatch, "GameType in the URL must match GameType in the request body"))).ToHttpResult();

            var response = await ((ICentralBanFileStatusApi)this).UpsertCentralBanFileStatus(upsertDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CentralBanFileStatusDto>> ICentralBanFileStatusApi.UpsertCentralBanFileStatus(UpsertCentralBanFileStatusDto upsertDto, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(upsertDto);

            var gtInt = (int)upsertDto.GameType;
            var entity = await context.CentralBanFileStatuses
                .FirstOrDefaultAsync(s => s.GameType == gtInt, cancellationToken)
                .ConfigureAwait(false);

            var created = false;
            if (entity is null)
            {
                entity = new XiCentralBanFileStatusEntity { GameType = gtInt };
                context.CentralBanFileStatuses.Add(entity);
                created = true;
            }

            upsertDto.ApplyTo(entity);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse<CentralBanFileStatusDto>(entity.ToDto()).ToApiResult(created ? HttpStatusCode.Created : HttpStatusCode.OK);
        }
    }
}
