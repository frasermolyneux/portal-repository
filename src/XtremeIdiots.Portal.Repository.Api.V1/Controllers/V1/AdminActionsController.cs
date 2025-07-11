using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.DataLib;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using Asp.Versioning;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class AdminActionsController : ControllerBase, IAdminActionsApi
    {
        private readonly PortalDbContext context;
        private readonly IMapper mapper;

        public AdminActionsController(PortalDbContext context, IMapper mapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [Route("admin-actions/{adminActionId}")]
        public async Task<IActionResult> GetAdminAction(Guid adminActionId, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).GetAdminAction(adminActionId, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<AdminActionDto>> IAdminActionsApi.GetAdminAction(Guid adminActionId, CancellationToken cancellationToken)
        {
            var adminAction = await context.AdminActions.FindAsync(new object[] { adminActionId }, cancellationToken);
            if (adminAction == null)
                return new ApiResult<AdminActionDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<AdminActionDto>(adminAction);
            return new ApiResponse<AdminActionDto>(result).ToApiResult();
        }

        [HttpGet]
        [Route("admin-actions")]
        public async Task<IActionResult> GetAdminActions(
            [FromQuery] GameType? gameType = null,
            [FromQuery] Guid? playerId = null,
            [FromQuery] string? adminId = null,
            [FromQuery] AdminActionFilter? filter = null,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            [FromQuery] AdminActionOrder? order = null,
            CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).GetAdminActions(gameType, playerId, adminId, filter, skipEntries, takeEntries, order, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<AdminActionDto>>> IAdminActionsApi.GetAdminActions(
            GameType? gameType,
            Guid? playerId,
            string? adminId,
            AdminActionFilter? filter,
            int skipEntries,
            int takeEntries,
            AdminActionOrder? order,
            CancellationToken cancellationToken)
        {
            var query = context.AdminActions.AsQueryable();

            if (playerId.HasValue)
                query = query.Where(a => a.PlayerId == playerId.Value);

            if (filter.HasValue)
            {
                switch (filter.Value)
                {
                    case AdminActionFilter.ActiveBans:
                        query = query.Where(a => a.Expires == null || a.Expires > DateTime.UtcNow);
                        break;
                    case AdminActionFilter.UnclaimedBans:
                        query = query.Where(a => a.Expires != null && a.Expires <= DateTime.UtcNow);
                        break;
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // Apply ordering
            if (order.HasValue)
            {
                switch (order.Value)
                {
                    case AdminActionOrder.CreatedAsc:
                        query = query.OrderBy(a => a.Created);
                        break;
                    case AdminActionOrder.CreatedDesc:
                        query = query.OrderByDescending(a => a.Created);
                        break;
                    default:
                        query = query.OrderByDescending(a => a.Created);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(a => a.Created);
            }

            var filteredCount = await query.CountAsync(cancellationToken);
            var adminActions = await query.Skip(skipEntries).Take(takeEntries).ToListAsync(cancellationToken);

            var result = new CollectionModel<AdminActionDto>
            {
                Items = adminActions.Select(mapper.Map<AdminActionDto>).ToList(),
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };

            return new ApiResponse<CollectionModel<AdminActionDto>>(result).ToApiResult();
        }

        [HttpPost]
        [Route("admin-actions")]
        public async Task<IActionResult> CreateAdminAction([FromBody] CreateAdminActionDto createAdminActionDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).CreateAdminAction(createAdminActionDto, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IAdminActionsApi.CreateAdminAction(CreateAdminActionDto createAdminActionDto, CancellationToken cancellationToken)
        {
            var adminAction = mapper.Map<AdminAction>(createAdminActionDto);
            context.AdminActions.Add(adminAction);
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        [HttpPut]
        [Route("admin-actions")]
        public async Task<IActionResult> UpdateAdminAction([FromBody] EditAdminActionDto editAdminActionDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).UpdateAdminAction(editAdminActionDto, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IAdminActionsApi.UpdateAdminAction(EditAdminActionDto editAdminActionDto, CancellationToken cancellationToken)
        {
            var adminAction = await context.AdminActions.FindAsync(new object[] { editAdminActionDto.AdminActionId }, cancellationToken);
            if (adminAction == null)
                return new ApiResult(HttpStatusCode.NotFound);

            mapper.Map(editAdminActionDto, adminAction);
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult();
        }

        [HttpDelete]
        [Route("admin-actions/{adminActionId}")]
        public async Task<IActionResult> DeleteAdminAction(Guid adminActionId, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).DeleteAdminAction(adminActionId, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IAdminActionsApi.DeleteAdminAction(Guid adminActionId, CancellationToken cancellationToken)
        {
            var adminAction = await context.AdminActions.FindAsync(new object[] { adminActionId }, cancellationToken);
            if (adminAction == null)
                return new ApiResult(HttpStatusCode.NotFound);

            context.AdminActions.Remove(adminAction);
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult();
        }
    }
}
