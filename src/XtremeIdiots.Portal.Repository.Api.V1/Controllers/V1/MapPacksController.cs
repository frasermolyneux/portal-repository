using System.Linq;
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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class MapPacksController : Controller, IMapPacksApi
    {
        private readonly PortalDbContext context;
        private readonly IMapper mapper;

        public MapPacksController(
            PortalDbContext context,
            IMapper mapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [Route("maps/pack/{mapPackId}")]
        public async Task<IActionResult> GetMapPack(Guid mapPackId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapPacksApi)this).GetMapPack(mapPackId, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<MapPackDto>> IMapPacksApi.GetMapPack(Guid mapPackId, CancellationToken cancellationToken)
        {
            var mapPack = await context.MapPacks
                .Include(mp => mp.GameMode)
                .Include(mp => mp.MapPackMaps)
                .SingleOrDefaultAsync(mp => mp.MapPackId == mapPackId && !mp.Deleted, cancellationToken);

            if (mapPack == null)
                return new ApiResult<MapPackDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<MapPackDto>(mapPack);
            return new ApiResult<MapPackDto>(HttpStatusCode.OK, new ApiResponse<MapPackDto>(result));
        }

        [HttpGet]
        [Route("maps/pack")]
        public async Task<IActionResult> GetMapPacks(string? gameTypes, string? gameServerIds, MapPacksFilter? filter, int? skipEntries, int? takeEntries, MapPacksOrder? order, CancellationToken cancellationToken = default)
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

            Guid[]? gameServerIdsFilter = null;
            if (!string.IsNullOrWhiteSpace(gameServerIds))
            {
                var split = gameServerIds.Split(",");
                gameServerIdsFilter = split.Select(id => Guid.Parse(id)).ToArray();
            }

            var response = await ((IMapPacksApi)this).GetMapPacks(gameTypesFilter, gameServerIdsFilter, filter, skipEntries.Value, takeEntries.Value, order, cancellationToken);

            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<MapPackDto>>> IMapPacksApi.GetMapPacks(GameType[]? gameTypes, Guid[]? gameServerIds, MapPacksFilter? filter, int skipEntries, int takeEntries, MapPacksOrder? order, CancellationToken cancellationToken)
        {
            var query = context.MapPacks.Include(gs => gs.MapPackMaps).Where(mp => !mp.Deleted).AsQueryable();
            query = ApplyFilter(query, gameTypes, null, null);
            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplyFilter(query, gameTypes, gameServerIds, filter);
            var filteredCount = await query.CountAsync(cancellationToken);

            query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
            var results = await query.ToListAsync(cancellationToken);

            var entries = results.Select(m => mapper.Map<MapPackDto>(m)).ToList();

            var result = new CollectionModel<MapPackDto>
            {
                TotalCount = totalCount,
                FilteredCount = filteredCount,
                Items = entries
            };

            return new ApiResult<CollectionModel<MapPackDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapPackDto>>(result));
        }

        async Task<ApiResult> IMapPacksApi.CreateMapPack(CreateMapPackDto createMapPackDto, CancellationToken cancellationToken)
        {
            var mapPack = mapper.Map<MapPack>(createMapPackDto);
            context.MapPacks.Add(mapPack);
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.Created, new ApiResponse());
        }

        [HttpPost]
        [Route("maps/pack")]
        public async Task<IActionResult> CreateMapPacks(CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            List<CreateMapPackDto>? createMapPackDtos;
            try
            {
                createMapPackDtos = JsonConvert.DeserializeObject<List<CreateMapPackDto>>(requestBody);
            }
            catch
            {
                return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();
            }

            if (createMapPackDtos == null || !createMapPackDtos.Any())
                return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();

            var response = await ((IMapPacksApi)this).CreateMapPacks(createMapPackDtos, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IMapPacksApi.CreateMapPacks(List<CreateMapPackDto> createMapPackDtos, CancellationToken cancellationToken)
        {
            foreach (var createMapPackDto in createMapPackDtos)
            {
                var mapPack = mapper.Map<MapPack>(createMapPackDto);
                var maps = await context.Maps.Where(m => createMapPackDto.MapIds.Contains(m.MapId)).ToListAsync(cancellationToken);
                mapPack.MapPackMaps = maps.Select(m => new MapPackMap { Map = m }).ToList();

                await context.MapPacks.AddAsync(mapPack, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.Created, new ApiResponse());
        }

        [HttpPatch]
        [Route("maps/pack/{mapPackId}")]
        public async Task<IActionResult> UpdateMapPack(Guid mapPackId, CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            UpdateMapPackDto? updateMapPackDto;
            try
            {
                updateMapPackDto = JsonConvert.DeserializeObject<UpdateMapPackDto>(requestBody);
            }
            catch
            {
                return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();
            }

            if (updateMapPackDto == null)
                return new ApiResult(HttpStatusCode.BadRequest).ToHttpResult();

            if (updateMapPackDto.MapPackId != mapPackId)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestEntityMismatch, ApiErrorMessages.RequestEntityMismatchMessage))).ToHttpResult();

            var response = await ((IMapPacksApi)this).UpdateMapPack(updateMapPackDto, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IMapPacksApi.UpdateMapPack(UpdateMapPackDto updateMapPackDto, CancellationToken cancellationToken)
        {
            var mapPack = await context.MapPacks.SingleOrDefaultAsync(mp => mp.MapPackId == updateMapPackDto.MapPackId, cancellationToken);

            if (mapPack == null)
                return new ApiResult(HttpStatusCode.NotFound, new ApiResponse());

            mapper.Map(updateMapPackDto, mapPack);

            if (updateMapPackDto.MapIds != null)
            {
                var maps = await context.Maps.Where(m => updateMapPackDto.MapIds.Contains(m.MapId)).ToListAsync(cancellationToken);
                mapPack.MapPackMaps = maps.Select(m => new MapPackMap { Map = m }).ToList();
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.OK, new ApiResponse());
        }

        [HttpDelete]
        [Route("maps/pack/{mapPackId}")]
        public async Task<IActionResult> DeleteMapPack(Guid mapPackId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapPacksApi)this).DeleteMapPack(mapPackId, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IMapPacksApi.DeleteMapPack(Guid mapPackId, CancellationToken cancellationToken)
        {
            var mapPack = await context.MapPacks.SingleOrDefaultAsync(mp => mp.MapPackId == mapPackId, cancellationToken);

            if (mapPack == null)
                return new ApiResult(HttpStatusCode.NotFound, new ApiResponse());

            mapPack.Deleted = true;
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.OK, new ApiResponse());
        }

        private IQueryable<MapPack> ApplyFilter(IQueryable<MapPack> query, GameType[]? gameTypes, Guid[]? gameServerIds, MapPacksFilter? filter)
        {
            if (gameTypes != null && gameTypes.Length > 0)
            {
                query = query.Include(mp => mp.GameServer).AsQueryable();
                var gameTypeInts = gameTypes.Select(gt => gt.ToGameTypeInt()).ToArray();
                query = query.Where(mp => mp.GameServer != null && gameTypeInts.Contains(mp.GameServer.GameType)).AsQueryable();
            }

            if (gameServerIds != null && gameServerIds.Length > 0)
            {
                query = query.Include(mp => mp.GameServer).AsQueryable();
                query = query.Where(mp => mp.GameServer != null && gameServerIds.Contains(mp.GameServer.GameServerId)).AsQueryable();
            }

            switch (filter)
            {
                case MapPacksFilter.SyncToGameServer:
                    query = query.Where(mp => mp.SyncToGameServer).AsQueryable();
                    break;
                case MapPacksFilter.NotSynced:
                    query = query.Where(mp => !mp.SyncCompleted).AsQueryable();
                    break;
            }

            return query;
        }

        private IQueryable<MapPack> ApplyOrderAndLimits(IQueryable<MapPack> query, int skipEntries, int takeEntries, MapPacksOrder? order)
        {
            switch (order)
            {
                case MapPacksOrder.Title:
                    query = query.OrderBy(mp => mp.Title).AsQueryable();
                    break;
                case MapPacksOrder.GameMode:
                    query = query.OrderBy(mp => mp.GameMode).AsQueryable();
                    break;
            }

            query = query.Skip(skipEntries).AsQueryable();
            query = query.Take(takeEntries).AsQueryable();

            return query;
        }
    }
}

