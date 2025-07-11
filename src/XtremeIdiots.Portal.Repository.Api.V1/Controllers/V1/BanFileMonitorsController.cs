using System.Net;
using Asp.Versioning;
using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

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
        public async Task<IActionResult> GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default)
        {
            var response = await ((IBanFileMonitorsApi)this).GetBanFileMonitor(banFileMonitorId, cancellationToken);

            return response.ToHttpResult();
        }

        async Task<ApiResult<BanFileMonitorDto>> IBanFileMonitorsApi.GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken)
        {
            var banFileMonitor = await context.BanFileMonitors
                .Include(bfm => bfm.GameServer)
                .Where(bfm => !bfm.GameServer.Deleted)
                .SingleOrDefaultAsync(bfm => bfm.BanFileMonitorId == banFileMonitorId, cancellationToken);

            if (banFileMonitor == null)
                return new ApiResult<BanFileMonitorDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<BanFileMonitorDto>(banFileMonitor);

            return new ApiResult<BanFileMonitorDto>(HttpStatusCode.OK, new ApiResponse<BanFileMonitorDto>(result));
        }

        [HttpGet]
        [Route("ban-file-monitors")]
        public async Task<IActionResult> GetBanFileMonitors(string? gameTypes, string? banFileMonitorIds, Guid? gameServerId, int? skipEntries, int? takeEntries, BanFileMonitorOrder? order, CancellationToken cancellationToken = default)
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

            var response = await ((IBanFileMonitorsApi)this).GetBanFileMonitors(gameTypesFilter, banFileMonitorsIdFilter, gameServerId, skipEntries.Value, takeEntries.Value, order, cancellationToken);

            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<BanFileMonitorDto>>> IBanFileMonitorsApi.GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order, CancellationToken cancellationToken)
        {
            var query = context.BanFileMonitors.Include(bfm => bfm.GameServer).Where(bfm => !bfm.GameServer.Deleted).AsQueryable();
            query = ApplyFilter(query, gameTypes, null, null);
            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplyFilter(query, gameTypes, banFileMonitorIds, gameServerId);
            var filteredCount = await query.CountAsync(cancellationToken);

            query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
            var results = await query.ToListAsync(cancellationToken);

            var entries = results.Select(bfm => mapper.Map<BanFileMonitorDto>(bfm)).ToList();

            var result = new CollectionModel<BanFileMonitorDto>
            {
                Items = entries,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };

            return new ApiResult<CollectionModel<BanFileMonitorDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<BanFileMonitorDto>>(result));
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
                return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();
            }

            if (createBanFileMonitorDto == null)
                return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();

            var response = await ((IBanFileMonitorsApi)this).CreateBanFileMonitor(createBanFileMonitorDto);

            return response.ToHttpResult();
        }

        async Task<ApiResult> IBanFileMonitorsApi.CreateBanFileMonitor(CreateBanFileMonitorDto createBanFileMonitorDto, CancellationToken cancellationToken)
        {
            var banFileMonitor = mapper.Map<BanFileMonitor>(createBanFileMonitorDto);
            banFileMonitor.LastSync = DateTime.UtcNow.AddHours(-4);

            await context.BanFileMonitors.AddRangeAsync(banFileMonitor);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK, new ApiResponse());
        }

        [HttpPatch]
        [Route("ban-file-monitors/{banFileMonitorId}")]
        public async Task<IActionResult> UpdateBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            EditBanFileMonitorDto? editBanFileMonitorDto;
            try
            {
                editBanFileMonitorDto = JsonConvert.DeserializeObject<EditBanFileMonitorDto>(requestBody);
            }
            catch (Exception)
            {
                return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();
            }

            if (editBanFileMonitorDto == null)
                return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();

            if (editBanFileMonitorDto.BanFileMonitorId != banFileMonitorId)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.EntityIdMismatch, ApiErrorMessages.BanFileMonitorIdMismatchMessage))).ToHttpResult();

            var response = await ((IBanFileMonitorsApi)this).UpdateBanFileMonitor(editBanFileMonitorDto, cancellationToken);

            return response.ToHttpResult();

        }

        async Task<ApiResult> IBanFileMonitorsApi.UpdateBanFileMonitor(EditBanFileMonitorDto editBanFileMonitorDto, CancellationToken cancellationToken)
        {
            var banFileMonitor = await context.BanFileMonitors.SingleOrDefaultAsync(bfm => bfm.BanFileMonitorId == editBanFileMonitorDto.BanFileMonitorId, cancellationToken);

            if (banFileMonitor == null)
                return new ApiResult(HttpStatusCode.NotFound);

            mapper.Map(editBanFileMonitorDto, banFileMonitor);

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK, new ApiResponse());
        }

        [HttpDelete]
        [Route("ban-file-monitors/{banFileMonitorId}")]
        public async Task<IActionResult> DeleteBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default)
        {
            var response = await ((IBanFileMonitorsApi)this).DeleteBanFileMonitor(banFileMonitorId, cancellationToken);

            return response.ToHttpResult();
        }

        async Task<ApiResult> IBanFileMonitorsApi.DeleteBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken)
        {
            var banFileMonitor = await context.BanFileMonitors
                .SingleOrDefaultAsync(bfm => bfm.BanFileMonitorId == banFileMonitorId, cancellationToken);

            if (banFileMonitor == null)
                return new ApiResult(HttpStatusCode.NotFound);

            context.Remove(banFileMonitor);

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK, new ApiResponse());
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

