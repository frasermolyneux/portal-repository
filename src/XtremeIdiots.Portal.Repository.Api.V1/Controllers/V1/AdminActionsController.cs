using System.Net;
using Asp.Versioning;
using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.WebExtensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class AdminActionsController : Controller, IAdminActionsApi
    {
        private readonly PortalDbContext context;
        private readonly IMapper mapper;

        public AdminActionsController(
            PortalDbContext context,
            IMapper mapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [Route("admin-actions/{adminActionId}")]
        public async Task<IActionResult> GetAdminAction(Guid adminActionId)
        {
            var response = await ((IAdminActionsApi)this).GetAdminAction(adminActionId);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<AdminActionDto>> IAdminActionsApi.GetAdminAction(Guid adminActionId)
        {
            var adminAction = await context.AdminActions
                .Include(aa => aa.Player)
                .Include(aa => aa.UserProfile)
                .SingleOrDefaultAsync(aa => aa.AdminActionId == adminActionId);

            if (adminAction == null)
                return new ApiResponseDto<AdminActionDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<AdminActionDto>(adminAction);

            return new ApiResponseDto<AdminActionDto>(HttpStatusCode.OK, result);
        }

        [HttpGet]
        [Route("admin-actions")]
        public async Task<IActionResult> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int? skipEntries, int? takeEntries, AdminActionOrder? order)
        {
            if (!skipEntries.HasValue)
                skipEntries = 0;

            if (!takeEntries.HasValue)
                takeEntries = 20;

            var response = await ((IAdminActionsApi)this).GetAdminActions(gameType, playerId, adminId, filter, skipEntries.Value, takeEntries.Value, order);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<AdminActionCollectionDto>> IAdminActionsApi.GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order)
        {
            var query = context.AdminActions.Include(aa => aa.Player).Include(aa => aa.UserProfile).AsQueryable();
            query = ApplyFilter(query, gameType, null, null, null);
            var totalCount = await query.CountAsync();

            query = ApplyFilter(query, gameType, playerId, adminId, filter);
            var filteredCount = await query.CountAsync();

            query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
            var results = await query.ToListAsync();

            var entries = results.Select(m => mapper.Map<AdminActionDto>(m)).ToList();

            var result = new AdminActionCollectionDto
            {
                TotalRecords = totalCount,
                FilteredRecords = filteredCount,
                Entries = entries
            };

            return new ApiResponseDto<AdminActionCollectionDto>(HttpStatusCode.OK, result);
        }

        [HttpPost]
        [Route("admin-actions")]
        public async Task<IActionResult> CreateAdminAction()
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            CreateAdminActionDto? createAdminActionDto;
            try
            {
                createAdminActionDto = JsonConvert.DeserializeObject<CreateAdminActionDto>(requestBody);
            }
            catch
            {
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
            }

            if (createAdminActionDto == null)
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null" }).ToHttpResult();

            var response = await ((IAdminActionsApi)this).CreateAdminAction(createAdminActionDto);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> IAdminActionsApi.CreateAdminAction(CreateAdminActionDto createAdminActionDto)
        {
            var adminAction = mapper.Map<AdminAction>(createAdminActionDto);

            if (!string.IsNullOrWhiteSpace(createAdminActionDto.AdminId))
                adminAction.UserProfile = await context.UserProfiles.SingleOrDefaultAsync(u => u.XtremeIdiotsForumId == createAdminActionDto.AdminId);

            adminAction.Created = DateTime.UtcNow;

            context.AdminActions.Add(adminAction);
            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }

        [HttpPatch]
        [Route("admin-actions/{adminActionId}")]
        public async Task<IActionResult> UpdateAdminAction(Guid adminActionId)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            EditAdminActionDto? editAdminActionDto;
            try
            {
                editAdminActionDto = JsonConvert.DeserializeObject<EditAdminActionDto>(requestBody);
            }
            catch
            {
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
            }

            if (editAdminActionDto == null)
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null" }).ToHttpResult();

            if (editAdminActionDto.AdminActionId != adminActionId)
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request entity identifiers did not match" }).ToHttpResult();

            var response = await ((IAdminActionsApi)this).UpdateAdminAction(editAdminActionDto);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> IAdminActionsApi.UpdateAdminAction(EditAdminActionDto editAdminActionDto)
        {
            var adminAction = await context.AdminActions
                .Include(aa => aa.UserProfile)
                .SingleOrDefaultAsync(aa => aa.AdminActionId == editAdminActionDto.AdminActionId);

            if (adminAction == null)
                return new ApiResponseDto(HttpStatusCode.NotFound);

            mapper.Map(editAdminActionDto, adminAction);

            if (!string.IsNullOrWhiteSpace(editAdminActionDto.AdminId) && editAdminActionDto.AdminId != adminAction.UserProfile?.XtremeIdiotsForumId)
                adminAction.UserProfile = await context.UserProfiles.SingleOrDefaultAsync(u => u.XtremeIdiotsForumId == editAdminActionDto.AdminId);

            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }


        [HttpDelete]
        [Route("admin-actions/{adminActionId}")]
        public async Task<IActionResult> DeleteAdminAction(Guid adminActionId)
        {
            var response = await ((IAdminActionsApi)this).DeleteAdminAction(adminActionId);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> IAdminActionsApi.DeleteAdminAction(Guid adminActionId)
        {
            var adminAction = await context.AdminActions
                .SingleOrDefaultAsync(aa => aa.AdminActionId == adminActionId);

            if (adminAction == null)
                return new ApiResponseDto(HttpStatusCode.NotFound);

            context.Remove(adminAction);

            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }

        private IQueryable<AdminAction> ApplyFilter(IQueryable<AdminAction> query, GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter)
        {
            if (gameType.HasValue)
                query = query.Where(aa => aa.Player.GameType == ((GameType)gameType).ToGameTypeInt()).AsQueryable();

            if (playerId.HasValue)
                query = query.Where(aa => aa.PlayerId == playerId).AsQueryable();

            if (!string.IsNullOrWhiteSpace(adminId))
                query = query.Where(aa => aa.UserProfile.XtremeIdiotsForumId == adminId).AsQueryable();

            switch (filter)
            {
                case AdminActionFilter.ActiveBans:
                    query = query.Where(aa => aa.Type == AdminActionType.Ban.ToAdminActionTypeInt() && aa.Expires == null || aa.Type == AdminActionType.TempBan.ToAdminActionTypeInt() && aa.Expires > DateTime.UtcNow).AsQueryable();
                    break;
                case AdminActionFilter.UnclaimedBans:
                    query = query.Where(aa => aa.Type == AdminActionType.Ban.ToAdminActionTypeInt() && aa.Expires == null && aa.UserProfile == null).AsQueryable();
                    break;
            }

            return query;
        }

        private IQueryable<AdminAction> ApplyOrderAndLimits(IQueryable<AdminAction> query, int skipEntries, int takeEntries, AdminActionOrder? order)
        {
            switch (order)
            {
                case AdminActionOrder.CreatedAsc:
                    query = query.OrderBy(aa => aa.Created).AsQueryable();
                    break;
                case AdminActionOrder.CreatedDesc:
                    query = query.OrderByDescending(aa => aa.Created).AsQueryable();
                    break;
            }

            query = query.Skip(skipEntries).AsQueryable();
            query = query.Take(takeEntries).AsQueryable();

            return query;
        }
    }
}
