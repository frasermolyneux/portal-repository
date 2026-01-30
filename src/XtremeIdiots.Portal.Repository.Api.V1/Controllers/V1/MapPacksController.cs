using System.Linq;
using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class MapPacksController : ControllerBase, IMapPacksApi
    {
        private readonly PortalDbContext context;

        public MapPacksController(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        /// <summary>
        /// Retrieves a specific map pack by its unique identifier.
        /// </summary>
        /// <param name="mapPackId">The unique identifier of the map pack to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The map pack details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("maps/pack/{mapPackId:guid}")]
        [ProducesResponseType<MapPackDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMapPack(Guid mapPackId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapPacksApi)this).GetMapPack(mapPackId, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific map pack by its unique identifier.
        /// </summary>
        /// <param name="mapPackId">The unique identifier of the map pack to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the map pack details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<MapPackDto>> IMapPacksApi.GetMapPack(Guid mapPackId, CancellationToken cancellationToken)
        {
            var mapPack = await context.MapPacks
                .Include(mp => mp.GameServer)
                .Include(mp => mp.MapPackMaps)
                .AsNoTracking()
                .FirstOrDefaultAsync(mp => mp.MapPackId == mapPackId && !mp.Deleted, cancellationToken);

            if (mapPack == null)
                return new ApiResult<MapPackDto>(HttpStatusCode.NotFound);

            var result = mapPack.ToDto();
            return new ApiResponse<MapPackDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a paginated list of map packs with optional filtering and sorting.
        /// </summary>
        /// <param name="gameTypes">Optional comma-separated list of game types to filter by.</param>
        /// <param name="gameServerIds">Optional comma-separated list of game server IDs to filter by.</param>
        /// <param name="filter">Optional filter criteria for map packs.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of map packs.</returns>
        [HttpGet("maps/pack")]
        [ProducesResponseType<CollectionModel<MapPackDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMapPacks(
            [FromQuery] string? gameTypes = null,
            [FromQuery] string? gameServerIds = null,
            [FromQuery] MapPacksFilter? filter = null,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            [FromQuery] MapPacksOrder? order = null,
            CancellationToken cancellationToken = default)
        {
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

            var response = await ((IMapPacksApi)this).GetMapPacks(gameTypesFilter, gameServerIdsFilter, filter, skipEntries, takeEntries, order, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of map packs with optional filtering and sorting.
        /// </summary>
        /// <param name="gameTypes">Optional array of game types to filter by.</param>
        /// <param name="gameServerIds">Optional array of game server IDs to filter by.</param>
        /// <param name="filter">Optional filter criteria for map packs.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of map packs.</returns>
        async Task<ApiResult<CollectionModel<MapPackDto>>> IMapPacksApi.GetMapPacks(
            GameType[]? gameTypes,
            Guid[]? gameServerIds,
            MapPacksFilter? filter,
            int skipEntries,
            int takeEntries,
            MapPacksOrder? order,
            CancellationToken cancellationToken)
        {
            var baseQuery = context.MapPacks
                .AsNoTracking()
                .Where(mp => !mp.Deleted);

            // Calculate total count before applying filters
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Apply filters
            var filteredQuery = ApplyFilters(baseQuery, gameTypes, gameServerIds, filter);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken);

            // Apply ordering and pagination
            var orderedQuery = ApplyOrderingAndPagination(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery
                .Include(mp => mp.MapPackMaps)
                .ToListAsync(cancellationToken);

            var entries = results.Select(m => m.ToDto()).ToList();

            var data = new CollectionModel<MapPackDto>(entries);

            return new ApiResponse<CollectionModel<MapPackDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Creates a new map pack.
        /// </summary>
        /// <param name="createMapPackDto">The map pack data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response indicating the map pack was created.</returns>
        [HttpPost("maps/pack-single")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMapPack([FromBody] CreateMapPackDto createMapPackDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapPacksApi)this).CreateMapPack(createMapPackDto, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new map pack.
        /// </summary>
        /// <param name="createMapPackDto">The map pack data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map pack was created.</returns>
        async Task<ApiResult> IMapPacksApi.CreateMapPack(CreateMapPackDto createMapPackDto, CancellationToken cancellationToken)
        {
            var mapPack = createMapPackDto.ToEntity();
            context.MapPacks.Add(mapPack);
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Creates multiple map packs in a single operation.
        /// </summary>
        /// <param name="createMapPackDtos">The collection of map pack data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response indicating the map packs were created.</returns>
        [HttpPost("maps/pack")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMapPacks([FromBody] List<CreateMapPackDto> createMapPackDtos, CancellationToken cancellationToken = default)
        {
            if (createMapPackDtos == null || !createMapPackDtos.Any())
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();

            var response = await ((IMapPacksApi)this).CreateMapPacks(createMapPackDtos, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates multiple map packs in a single operation.
        /// </summary>
        /// <param name="createMapPackDtos">The collection of map pack data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map packs were created.</returns>
        async Task<ApiResult> IMapPacksApi.CreateMapPacks(List<CreateMapPackDto> createMapPackDtos, CancellationToken cancellationToken)
        {
            // Get all required map IDs at once to avoid N+1 queries
            var allMapIds = createMapPackDtos.SelectMany(dto => dto.MapIds).Distinct().ToList();
            var allMaps = await context.Maps
                .AsNoTracking()
                .Where(m => allMapIds.Contains(m.MapId))
                .ToListAsync(cancellationToken);

            var mapLookup = allMaps.ToDictionary(m => m.MapId, m => m);

            foreach (var createMapPackDto in createMapPackDtos)
            {
                var mapPack = createMapPackDto.ToEntity();
                var maps = createMapPackDto.MapIds
                    .Where(id => mapLookup.ContainsKey(id))
                    .Select(id => mapLookup[id])
                    .ToList();
                mapPack.MapPackMaps = maps.Select(m => new MapPackMap { Map = m }).ToList();

                await context.MapPacks.AddAsync(mapPack, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Updates an existing map pack.
        /// </summary>
        /// <param name="mapPackId">The unique identifier of the map pack to update.</param>
        /// <param name="updateMapPackDto">The map pack data to update.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map pack was updated; otherwise, a 404 Not Found response.</returns>
        [HttpPatch("maps/pack/{mapPackId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMapPack(Guid mapPackId, [FromBody] UpdateMapPackDto updateMapPackDto, CancellationToken cancellationToken = default)
        {
            if (updateMapPackDto == null)
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();

            if (updateMapPackDto.MapPackId != mapPackId)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestEntityMismatch, ApiErrorMessages.RequestEntityMismatchMessage))).ToHttpResult();

            var response = await ((IMapPacksApi)this).UpdateMapPack(updateMapPackDto, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates an existing map pack.
        /// </summary>
        /// <param name="updateMapPackDto">The map pack data to update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map pack was updated if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapPacksApi.UpdateMapPack(UpdateMapPackDto updateMapPackDto, CancellationToken cancellationToken)
        {
            var mapPack = await context.MapPacks
                .FirstOrDefaultAsync(mp => mp.MapPackId == updateMapPackDto.MapPackId, cancellationToken);

            if (mapPack == null)
                return new ApiResult(HttpStatusCode.NotFound);

            updateMapPackDto.ApplyTo(mapPack);

            if (updateMapPackDto.MapIds != null)
            {
                var maps = await context.Maps
                    .AsNoTracking()
                    .Where(m => updateMapPackDto.MapIds.Contains(m.MapId))
                    .ToListAsync(cancellationToken);
                mapPack.MapPackMaps = maps.Select(m => new MapPackMap { Map = m }).ToList();
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Deletes a map pack by its unique identifier (marks as deleted).
        /// </summary>
        /// <param name="mapPackId">The unique identifier of the map pack to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map pack was deleted; otherwise, a 404 Not Found response.</returns>
        [HttpDelete("maps/pack/{mapPackId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMapPack(Guid mapPackId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapPacksApi)this).DeleteMapPack(mapPackId, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Deletes a map pack by its unique identifier (marks as deleted).
        /// </summary>
        /// <param name="mapPackId">The unique identifier of the map pack to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map pack was deleted if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapPacksApi.DeleteMapPack(Guid mapPackId, CancellationToken cancellationToken)
        {
            var mapPack = await context.MapPacks
                .FirstOrDefaultAsync(mp => mp.MapPackId == mapPackId, cancellationToken);

            if (mapPack == null)
                return new ApiResult(HttpStatusCode.NotFound);

            mapPack.Deleted = true;
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult();
        }

        private IQueryable<MapPack> ApplyFilters(IQueryable<MapPack> query, GameType[]? gameTypes, Guid[]? gameServerIds, MapPacksFilter? filter)
        {
            var needsGameServerInclude = (gameTypes != null && gameTypes.Length > 0) || (gameServerIds != null && gameServerIds.Length > 0);

            if (needsGameServerInclude)
            {
                query = query.Include(mp => mp.GameServer);
            }

            if (gameTypes != null && gameTypes.Length > 0)
            {
                var gameTypeInts = gameTypes.Select(gt => gt.ToGameTypeInt()).ToArray();
                query = query.Where(mp => mp.GameServer != null && gameTypeInts.Contains(mp.GameServer.GameType));
            }

            if (gameServerIds != null && gameServerIds.Length > 0)
            {
                query = query.Where(mp => mp.GameServer != null && gameServerIds.Contains(mp.GameServer.GameServerId));
            }

            return filter switch
            {
                MapPacksFilter.SyncToGameServer => query.Where(mp => mp.SyncToGameServer),
                MapPacksFilter.NotSynced => query.Where(mp => !mp.SyncCompleted),
                _ => query
            };
        }

        private IQueryable<MapPack> ApplyOrderingAndPagination(IQueryable<MapPack> query, int skipEntries, int takeEntries, MapPacksOrder? order)
        {
            var orderedQuery = order switch
            {
                MapPacksOrder.Title => query.OrderBy(mp => mp.Title),
                MapPacksOrder.GameMode => query.OrderBy(mp => mp.GameMode),
                _ => query.OrderBy(mp => mp.Title)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }
    }
}

