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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class BanFileMonitorsController : Controller, IBanFileMonitorsApi
    {
        private readonly PortalDbContext context;
        private readonly IMapper mapper;

        public BanFileMonitorsController(
            PortalDbContext context,
            IMapper mapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [Route("ban-file-monitors/{banFileMonitorId}")]
        public async Task<IActionResult> GetBanFileMonitor(Guid banFileMonitorId)
        {
            var response = await ((IBanFileMonitorsApi)this).GetBanFileMonitor(banFileMonitorId);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<BanFileMonitorDto>> IBanFileMonitorsApi.GetBanFileMonitor(Guid banFileMonitorId)
        {
            var banFileMonitor = await context.BanFileMonitors
                .Include(bfm => bfm.GameServer)
                .Where(bfm => !bfm.GameServer.Deleted)
                .SingleOrDefaultAsync(bfm => bfm.BanFileMonitorId == banFileMonitorId);

            if (banFileMonitor == null)
                return new ApiResponseDto<BanFileMonitorDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<BanFileMonitorDto>(banFileMonitor);

            return new ApiResponseDto<BanFileMonitorDto>(HttpStatusCode.OK, result);
        }

        [HttpGet]
        [Route("ban-file-monitors")]
        public async Task<IActionResult> GetBanFileMonitors(string? gameTypes, string? banFileMonitorIds, Guid? gameServerId, int? skipEntries, int? takeEntries, BanFileMonitorOrder? order)
        {
            if (!skipEntries.HasValue)
                skipEntries = 0;

            if (!takeEntries.HasValue)
                takeEntries = 20;

            GameType[]? gameTypesFilter = null;
            if (!string.IsNullOrWhiteSpace(gameTypes))
            {
                var split = gameTypes.Split(",");
                gameTypesFilter = split.Select(gt => Enum.Parse<GameType>(gt)).ToArray();
            }

            Guid[]? banFileMonitorsIdFilter = null;
            if (!string.IsNullOrWhiteSpace(banFileMonitorIds))
            {
                var split = banFileMonitorIds.Split(",");
                banFileMonitorsIdFilter = split.Select(id => Guid.Parse(id)).ToArray();
            }

            var response = await ((IBanFileMonitorsApi)this).GetBanFileMonitors(gameTypesFilter, banFileMonitorsIdFilter, gameServerId, skipEntries.Value, takeEntries.Value, order);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<BanFileMonitorCollectionDto>> IBanFileMonitorsApi.GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order)
        {
            var query = context.BanFileMonitors.Include(bfm => bfm.GameServer).Where(bfm => !bfm.GameServer.Deleted).AsQueryable();
            query = ApplyFilter(query, gameTypes, null, null);
            var totalCount = await query.CountAsync();

            query = ApplyFilter(query, gameTypes, banFileMonitorIds, gameServerId);
            var filteredCount = await query.CountAsync();

            query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
            var results = await query.ToListAsync();

            var entries = results.Select(bfm => mapper.Map<BanFileMonitorDto>(bfm)).ToList();

            var result = new BanFileMonitorCollectionDto
            {
                TotalRecords = totalCount,
                FilteredRecords = filteredCount,
                Entries = entries
            };

            return new ApiResponseDto<BanFileMonitorCollectionDto>(HttpStatusCode.OK, result);
        }

        [HttpPost]
        [Route("ban-file-monitors")]
        public async Task<IActionResult> CreateBanFileMonitor()
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            CreateBanFileMonitorDto? createBanFileMonitorDto;
            try
            {
                createBanFileMonitorDto = JsonConvert.DeserializeObject<CreateBanFileMonitorDto>(requestBody);
            }
            catch
            {
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
            }

            if (createBanFileMonitorDto == null)
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null" }).ToHttpResult();

            var response = await ((IBanFileMonitorsApi)this).CreateBanFileMonitor(createBanFileMonitorDto);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> IBanFileMonitorsApi.CreateBanFileMonitor(CreateBanFileMonitorDto createBanFileMonitorDto)
        {
            var banFileMonitor = mapper.Map<BanFileMonitor>(createBanFileMonitorDto);
            banFileMonitor.LastSync = DateTime.UtcNow.AddHours(-4);

            await context.BanFileMonitors.AddRangeAsync(banFileMonitor);
            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }

        [HttpPatch]
        [Route("ban-file-monitors/{banFileMonitorId}")]
        public async Task<IActionResult> UpdateBanFileMonitor(Guid banFileMonitorId)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            EditBanFileMonitorDto? editBanFileMonitorDto;
            try
            {
                editBanFileMonitorDto = JsonConvert.DeserializeObject<EditBanFileMonitorDto>(requestBody);
            }
            catch (Exception)
            {
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
            }

            if (editBanFileMonitorDto == null)
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null" }).ToHttpResult();

            if (editBanFileMonitorDto.BanFileMonitorId != banFileMonitorId)
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request entity identifiers did not match" }).ToHttpResult();

            var response = await ((IBanFileMonitorsApi)this).UpdateBanFileMonitor(editBanFileMonitorDto);

            return response.ToHttpResult();

        }

        async Task<ApiResponseDto> IBanFileMonitorsApi.UpdateBanFileMonitor(EditBanFileMonitorDto editBanFileMonitorDto)
        {
            var banFileMonitor = await context.BanFileMonitors.SingleOrDefaultAsync(bfm => bfm.BanFileMonitorId == editBanFileMonitorDto.BanFileMonitorId);

            if (banFileMonitor == null)
                return new ApiResponseDto(HttpStatusCode.NotFound);

            mapper.Map(editBanFileMonitorDto, banFileMonitor);

            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }

        [HttpDelete]
        [Route("ban-file-monitors/{banFileMonitorId}")]
        public async Task<IActionResult> DeleteBanFileMonitor(Guid banFileMonitorId)
        {
            var response = await ((IBanFileMonitorsApi)this).DeleteBanFileMonitor(banFileMonitorId);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> IBanFileMonitorsApi.DeleteBanFileMonitor(Guid banFileMonitorId)
        {
            var banFileMonitor = await context.BanFileMonitors
                .SingleOrDefaultAsync(bfm => bfm.BanFileMonitorId == banFileMonitorId);

            if (banFileMonitor == null)
                return new ApiResponseDto(HttpStatusCode.NotFound);

            context.Remove(banFileMonitor);

            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }

        private IQueryable<BanFileMonitor> ApplyFilter(IQueryable<BanFileMonitor> query, GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId)
        {
            if (gameTypes != null && gameTypes.Length > 0)
            {
                var gameTypeInts = gameTypes.Select(gt => gt.ToGameTypeInt()).ToArray();
                query = query.Where(bfm => gameTypeInts.Contains(bfm.GameServer.GameType)).AsQueryable();
            }

            if (banFileMonitorIds != null && banFileMonitorIds.Length > 0)
                query = query.Where(bfm => banFileMonitorIds.Contains(bfm.BanFileMonitorId)).AsQueryable();

            if (gameServerId.HasValue)
                query = query.Where(bfm => bfm.GameServerId == gameServerId).AsQueryable();

            return query;
        }

        private IQueryable<BanFileMonitor> ApplyOrderAndLimits(IQueryable<BanFileMonitor> query, int skipEntries, int takeEntries, BanFileMonitorOrder? order)
        {
            switch (order)
            {
                case BanFileMonitorOrder.BannerServerListPosition:
                    query = query.OrderBy(bfm => bfm.GameServer.ServerListPosition).AsQueryable();
                    break;
                case BanFileMonitorOrder.GameType:
                    query = query.OrderBy(bfm => bfm.GameServer.GameType).AsQueryable();
                    break;
            }

            query = query.Skip(skipEntries).AsQueryable();
            query = query.Take(takeEntries).AsQueryable();

            return query;
        }
    }
}
