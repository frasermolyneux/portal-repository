using System.Linq;
using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.WebExtensions;
using Newtonsoft.Json;
using XtremeIdiots.Portal.DataLib;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.MapPacks;
using XtremeIdiots.Portal.RepositoryWebApi.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
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
        public async Task<IActionResult> GetMapPack(Guid mapPackId)
        {
            var response = await ((IMapPacksApi)this).GetMapPack(mapPackId);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<MapPackDto>> IMapPacksApi.GetMapPack(Guid mapPackId)
        {
            var mapPack = await context.MapPacks
                .Include(mp => mp.GameMode)
                .Include(mp => mp.MapPackMaps)
                .SingleOrDefaultAsync(mp => mp.MapPackId == mapPackId && !mp.Deleted);

            if (mapPack == null)
                return new ApiResponseDto<MapPackDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<MapPackDto>(mapPack);

            return new ApiResponseDto<MapPackDto>(HttpStatusCode.OK, result);
        }

        [HttpGet]
        [Route("maps/pack")]
        public async Task<IActionResult> GetMapPacks(string? gameTypes, string? gameServerIds, MapPacksFilter? filter, int? skipEntries, int? takeEntries, MapPacksOrder? order)
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

            var response = await ((IMapPacksApi)this).GetMapPacks(gameTypesFilter, gameServerIdsFilter, filter, skipEntries.Value, takeEntries.Value, order);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<MapPackCollectionDto>> IMapPacksApi.GetMapPacks(GameType[]? gameTypes, Guid[]? gameServerIds, MapPacksFilter? filter, int skipEntries, int takeEntries, MapPacksOrder? order)
        {
            var query = context.MapPacks.Include(gs => gs.MapPackMaps).Where(mp => !mp.Deleted).AsQueryable();
            query = ApplyFilter(query, gameTypes, null, null);
            var totalCount = await query.CountAsync();

            query = ApplyFilter(query, gameTypes, gameServerIds, filter);
            var filteredCount = await query.CountAsync();

            query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
            var results = await query.ToListAsync();

            var entries = results.Select(m => mapper.Map<MapPackDto>(m)).ToList();

            var result = new MapPackCollectionDto
            {
                TotalRecords = totalCount,
                FilteredRecords = filteredCount,
                Entries = entries
            };

            return new ApiResponseDto<MapPackCollectionDto>(HttpStatusCode.OK, result);
        }

        Task<ApiResponseDto> IMapPacksApi.CreateMapPack(CreateMapPackDto createMapPackDto)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("maps/pack")]
        public async Task<IActionResult> CreateMapPacks()
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            List<CreateMapPackDto>? createMapPackDtos;
            try
            {
                createMapPackDtos = JsonConvert.DeserializeObject<List<CreateMapPackDto>>(requestBody);
            }
            catch
            {
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
            }

            if (createMapPackDtos == null || !createMapPackDtos.Any())
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null or did not contain any entries" }).ToHttpResult();

            var response = await ((IMapPacksApi)this).CreateMapPacks(createMapPackDtos);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> IMapPacksApi.CreateMapPacks(List<CreateMapPackDto> createMapPackDtos)
        {
            var mapPacks = createMapPackDtos.Select(mp => mapper.Map<MapPack>(mp)).ToList();

            foreach (var createMapPackDto in createMapPackDtos)
            {
                var mapPack = mapper.Map<MapPack>(createMapPackDto);
                var maps = await context.Maps.Where(m => createMapPackDto.MapIds.Contains(m.MapId)).ToListAsync();
                mapPack.MapPackMaps = maps.Select(m => new MapPackMap { Map = m }).ToList();

                await context.MapPacks.AddAsync(mapPack);
            }

            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }

        [HttpPatch]
        [Route("maps/pack/{mapPackId}")]
        public async Task<IActionResult> UpdateMapPack(Guid mapPackId)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            UpdateMapPackDto? updateMapPackDto;
            try
            {
                updateMapPackDto = JsonConvert.DeserializeObject<UpdateMapPackDto>(requestBody);
            }
            catch
            {
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
            }

            if (updateMapPackDto == null)
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null" }).ToHttpResult();

            if (updateMapPackDto.MapPackId != mapPackId)
                return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request entity identifiers did not match" }).ToHttpResult();

            var response = await ((IMapPacksApi)this).UpdateMapPack(updateMapPackDto);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> IMapPacksApi.UpdateMapPack(UpdateMapPackDto updateMapPackDto)
        {
            var mapPack = await context.MapPacks.SingleOrDefaultAsync(mp => mp.MapPackId == updateMapPackDto.MapPackId);

            if (mapPack == null)
                return new ApiResponseDto(HttpStatusCode.NotFound);

            mapper.Map(updateMapPackDto, mapPack);

            if (updateMapPackDto.MapIds != null)
            {
                var maps = await context.Maps.Where(m => updateMapPackDto.MapIds.Contains(m.MapId)).ToListAsync();
                mapPack.MapPackMaps = maps.Select(m => new MapPackMap { Map = m }).ToList();
            }

            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }

        [HttpDelete]
        [Route("maps/pack/{mapPackId}")]
        public async Task<IActionResult> DeleteMapPack(Guid mapPackId)
        {
            var response = await ((IMapPacksApi)this).DeleteMapPack(mapPackId);

            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> IMapPacksApi.DeleteMapPack(Guid mapPackId)
        {
            var mapPack = await context.MapPacks.SingleOrDefaultAsync(mp => mp.MapPackId == mapPackId);

            if (mapPack == null)
                return new ApiResponseDto(HttpStatusCode.NotFound);

            mapPack.Deleted = true;

            await context.SaveChangesAsync();

            return new ApiResponseDto(HttpStatusCode.OK);
        }

        private IQueryable<MapPack> ApplyFilter(IQueryable<MapPack> query, GameType[]? gameTypes, Guid[]? gameServerIds, MapPacksFilter? filter)
        {
            if (gameTypes != null && gameTypes.Length > 0)
            {
                query = query.Include(mp => mp.GameServer).AsQueryable();
                var gameTypeInts = gameTypes.Select(gt => gt.ToGameTypeInt()).ToArray();
                query = query.Where(mp => gameTypeInts.Contains(mp.GameServer.GameType)).AsQueryable();
            }

            if (gameServerIds != null && gameServerIds.Length > 0)
            {
                query = query.Include(mp => mp.GameServer).AsQueryable();
                query = query.Where(mp => gameServerIds.Contains(mp.GameServer.GameServerId)).AsQueryable();
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
