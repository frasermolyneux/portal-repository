using System.Net;
using Asp.Versioning;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.Api.V1.Services.Caching;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("v{version:apiVersion}")]
    public class MapsController : Controller, IMapsApi
    {
        private readonly PortalDbContext context;
        private readonly ILogger<MapsController> logger;
        private readonly IMapReadService mapReadService;
        private readonly IRepositoryCacheInvalidator cacheInvalidator;

        public MapsController(
            PortalDbContext context,
            ILogger<MapsController> logger,
            IMapReadService mapReadService,
            IRepositoryCacheInvalidator cacheInvalidator)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
            ArgumentNullException.ThrowIfNull(logger);
            this.logger = logger;
            ArgumentNullException.ThrowIfNull(mapReadService);
            this.mapReadService = mapReadService;
            ArgumentNullException.ThrowIfNull(cacheInvalidator);
            this.cacheInvalidator = cacheInvalidator;
        }

        [HttpGet("maps/games")]
        [ProducesResponseType<MapGamesDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMapGames(CancellationToken cancellationToken = default)
            => (await ((IMapsApi)this).GetMapGames(cancellationToken).ConfigureAwait(false)).ToHttpResult();

        async Task<ApiResult<MapGamesDto>> IMapsApi.GetMapGames(CancellationToken cancellationToken)
        {
            var games = await context.Maps.AsNoTracking().Where(m => m.GameType != (int)GameType.CallOfDuty4x && m.MapName != null && m.MapName != "")
                .Select(m => m.GameType)
                .Distinct().OrderBy(game => game).ToListAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<MapGamesDto>(new MapGamesDto
            {
                Games = games.Select(game => (GameType)game).Where(game => game != GameType.Unknown && Enum.IsDefined(game)).ToList()
            }).ToApiResult();
        }

        /// <summary>
        /// Retrieves a specific map by its unique identifier.
        /// </summary>
        /// <param name="mapId">The unique identifier of the map to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The map details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("maps/{mapId:guid}")]
        [ProducesResponseType<MapDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMap(Guid mapId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).GetMap(mapId, cancellationToken).ConfigureAwait(false);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific map by game type and map name.
        /// </summary>
        /// <param name="gameType">The game type of the map to retrieve.</param>
        /// <param name="mapName">The name of the map to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The map details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("maps/{gameType}/{mapName}")]
        [ProducesResponseType<MapDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMap(GameType gameType, string mapName, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).GetMap(gameType, mapName, cancellationToken).ConfigureAwait(false);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific map by its unique identifier.
        /// </summary>
        /// <param name="mapId">The unique identifier of the map to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the map details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<MapDto>> IMapsApi.GetMap(Guid mapId, CancellationToken cancellationToken)
        {
            return await mapReadService.GetMapByIdAsync(mapId, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a specific map by game type and map name.
        /// </summary>
        /// <param name="gameType">The game type of the map to retrieve.</param>
        /// <param name="mapName">The name of the map to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the map details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<MapDto>> IMapsApi.GetMap(GameType gameType, string mapName, CancellationToken cancellationToken)
        {
            return await mapReadService.GetMapByGameTypeAndNameAsync(gameType, mapName, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a paginated list of maps with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="mapNames">Optional comma-separated list of map names to filter by.</param>
        /// <param name="filter">Optional filter criteria for maps.</param>
        /// <param name="filterString">Optional string to filter map names by.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of maps.</returns>
        [HttpGet("maps")]
        [ProducesResponseType<CollectionModel<MapDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMaps(GameType? gameType, string? mapNames, MapsFilter? filter, string? filterString, int? skipEntries, int? takeEntries, MapsOrder? order, CancellationToken cancellationToken = default)
        {
            if (!skipEntries.HasValue)
            {
                skipEntries = 0;
            }

            if (!takeEntries.HasValue)
            {
                takeEntries = 20;
            }

            string[]? mapNamesFilter = null;
            if (!string.IsNullOrWhiteSpace(mapNames))
            {
                var split = mapNames.Split(",");
                mapNamesFilter = split.Select(mn => mn.Trim()).ToArray();
            }

            var response = await ((IMapsApi)this).GetMaps(gameType, mapNamesFilter, filter, filterString, skipEntries.Value, takeEntries.Value, order, cancellationToken).ConfigureAwait(false);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of maps with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="mapNames">Optional array of map names to filter by.</param>
        /// <param name="filter">Optional filter criteria for maps.</param>
        /// <param name="filterString">Optional string to filter map names by.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of maps.</returns>
        async Task<ApiResult<CollectionModel<MapDto>>> IMapsApi.GetMaps(GameType? gameType, string[]? mapNames, MapsFilter? filter, string? filterString, int skipEntries, int takeEntries, MapsOrder? order, CancellationToken cancellationToken)
        {
            var baseQuery = MapReadService.CatalogueQuery(context)
                .Where(m => m.GameType != (int)GameType.CallOfDuty4x);

            // Calculate total count before applying filters
            var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply filters
            var filteredQuery = ApplyFilter(baseQuery, gameType, mapNames, filter, filterString);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply ordering and pagination
            var orderedQuery = ApplyOrderAndLimits(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = results.Select(m => m.ToDto()).ToList();

            var data = new CollectionModel<MapDto>(entries);

            return new ApiResponse<CollectionModel<MapDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Creates a new map.
        /// </summary>
        /// <param name="createMapDto">The map data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map was created; otherwise, appropriate error responses.</returns>
        [HttpPost("map")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateMap([FromBody] CreateMapDto createMapDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).CreateMap(createMapDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new map.
        /// </summary>
        /// <param name="createMapDto">The map data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map was created if successful; otherwise, a 409 Conflict response.</returns>
        async Task<ApiResult> IMapsApi.CreateMap(CreateMapDto createMapDto, CancellationToken cancellationToken)
        {
            return await ((IMapsApi)this).CreateMaps([createMapDto], cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates multiple maps in a single operation.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if all maps were created; otherwise, appropriate error responses.</returns>
        [HttpPost("maps")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateMaps(CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync().ConfigureAwait(false);

            List<CreateMapDto>? createMapDtos;
            try
            {
                createMapDtos = JsonConvert.DeserializeObject<List<CreateMapDto>>(requestBody);
            }
            catch
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.DeserializationError, ApiErrorMessages.InvalidRequestBodyMessage));
                return err.ToBadRequestResult().ToHttpResult();
            }

            if (createMapDtos == null || !createMapDtos.Any())
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage));
                return err.ToBadRequestResult().ToHttpResult();
            }

            var response = await ((IMapsApi)this).CreateMaps(createMapDtos, cancellationToken).ConfigureAwait(false);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates multiple maps in a single operation.
        /// </summary>
        /// <param name="createMapDtos">The list of map data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating all maps were created if successful; otherwise, a 409 Conflict response.</returns>
        async Task<ApiResult> IMapsApi.CreateMaps(List<CreateMapDto> createMapDtos, CancellationToken cancellationToken)
        {
            if (createMapDtos.Any(dto => dto == null || string.IsNullOrWhiteSpace(dto.MapName)))
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.DeserializationError, ApiErrorMessages.InvalidRequestBodyMessage)).ToBadRequestResult();
            }

            Guid[] createdIds = [];
            async Task<ApiResult> CreateAsync(CancellationToken token)
            {
                context.ChangeTracker.Clear();
                createdIds = [];
                if (context.Database.IsSqlServer())
                {
                    await context.Database.ExecuteSqlRawAsync("""
                        DECLARE @result int;
                        EXEC @result = sys.sp_getapplock
                            @Resource = N'Portal.MapCatalogue',
                            @LockMode = N'Exclusive',
                            @LockOwner = N'Transaction',
                            @LockTimeout = 30000;
                        IF @result < 0 THROW 51000, 'Unable to lock the map catalogue.', 1;
                        """, token).ConfigureAwait(false);
                }

                var namesByGame = new Dictionary<GameType, HashSet<string>>();
                foreach (var dto in createMapDtos)
                {
                    var mapGameType = dto.GameType.GetMapGameType();
                    if (!namesByGame.TryGetValue(mapGameType, out var names))
                    {
                        namesByGame[mapGameType] = names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }

                    if (!names.Add(dto.MapName.TrimEnd(' ')) ||
                        await context.Maps.AnyAsync(m => m.GameType == (int)mapGameType && m.MapName == dto.MapName, token).ConfigureAwait(false))
                    {
                        var response = new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.MapConflictMessage));
                        return response.ToApiResult(HttpStatusCode.Conflict);
                    }
                }

                var maps = createMapDtos.Select(dto => dto.ToEntity()).ToArray();
                context.Maps.AddRange(maps);
                await context.SaveChangesAsync(token).ConfigureAwait(false);
                createdIds = maps.Select(map => map.MapId).ToArray();
                return new ApiResponse().ToApiResult(HttpStatusCode.Created);
            }

            if (!context.Database.IsRelational())
            {
                return await CreateAsync(cancellationToken).ConfigureAwait(false);
            }

            return await context.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
                CreateAsync,
                async token => createdIds.Length > 0 &&
                    await context.Maps.AsNoTracking().CountAsync(map => createdIds.Contains(map.MapId), token).ConfigureAwait(false) == createdIds.Length,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Partially updates an existing map. Only non-null properties are applied.
        /// </summary>
        /// <param name="mapId">The unique identifier of the map to update.</param>
        /// <param name="editMapDto">The map data to update.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map was updated; otherwise, appropriate error responses.</returns>
        [HttpPatch("maps/{mapId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMap([FromRoute] Guid mapId, [FromBody] EditMapDto editMapDto, CancellationToken cancellationToken = default)
        {
            if (editMapDto.MapId != mapId)
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.EntityIdMismatch, ApiErrorMessages.RequestEntityMismatchMessage));
                return err.ToBadRequestResult().ToHttpResult();
            }
            var response = await ((IMapsApi)this).UpdateMap(editMapDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates an existing map.
        /// </summary>
        /// <param name="editMapDto">The map data to update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map was updated if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapsApi.UpdateMap(EditMapDto editMapDto, CancellationToken cancellationToken)
        {
            var map = await context.Maps.FirstOrDefaultAsync(m => m.MapId == editMapDto.MapId, cancellationToken).ConfigureAwait(false);
            if (map == null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            editMapDto.ApplyTo(map);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await cacheInvalidator.InvalidateMapAsync(editMapDto.MapId, cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Partially updates multiple maps in a single operation. Only non-null properties are applied for each map.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if all maps were updated; otherwise, appropriate error responses.</returns>
        [HttpPatch("maps")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMaps(CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync().ConfigureAwait(false);

            List<EditMapDto>? editMapDtos;
            try
            {
                editMapDtos = JsonConvert.DeserializeObject<List<EditMapDto>>(requestBody);
            }
            catch
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.DeserializationError, ApiErrorMessages.InvalidRequestBodyMessage));
                return err.ToBadRequestResult().ToHttpResult();
            }

            if (editMapDtos == null || !editMapDtos.Any())
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage));
                return err.ToBadRequestResult().ToHttpResult();
            }

            var response = await ((IMapsApi)this).UpdateMaps(editMapDtos, cancellationToken).ConfigureAwait(false);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates multiple maps in a single operation.
        /// </summary>
        /// <param name="editMapDtos">The list of map data to update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating all maps were updated if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapsApi.UpdateMaps(List<EditMapDto> editMapDtos, CancellationToken cancellationToken)
        {
            foreach (var editMapDto in editMapDtos)
            {
                var map = await context.Maps.FirstOrDefaultAsync(m => m.MapId == editMapDto.MapId, cancellationToken).ConfigureAwait(false);
                if (map == null)
                {
                    return new ApiResult(HttpStatusCode.NotFound);
                }

                editMapDto.ApplyTo(map);
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await Task.WhenAll(editMapDtos.Select(dto => cacheInvalidator.InvalidateMapAsync(dto.MapId, cancellationToken))).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Deletes a map by its unique identifier.
        /// </summary>
        /// <param name="mapId">The unique identifier of the map to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map was deleted; otherwise, a 404 Not Found response.</returns>
        [HttpDelete("maps/{mapId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMap(Guid mapId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).DeleteMap(mapId, cancellationToken).ConfigureAwait(false);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Deletes a map by its unique identifier.
        /// </summary>
        /// <param name="mapId">The unique identifier of the map to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map was deleted if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapsApi.DeleteMap(Guid mapId, CancellationToken cancellationToken)
        {
            var map = await context.Maps
                .FirstOrDefaultAsync(m => m.MapId == mapId, cancellationToken).ConfigureAwait(false);

            if (map == null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            context.Remove(map);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await cacheInvalidator.InvalidateMapAsync(mapId, cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Retrieves a paginated list of map votes with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="mapId">Optional filter by specific map.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of map votes.</returns>
        [HttpGet("maps/votes")]
        [ProducesResponseType<CollectionModel<MapVoteDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMapVotes(GameType? gameType, Guid? mapId, int? skipEntries, int? takeEntries, MapVotesOrder? order, CancellationToken cancellationToken = default)
        {
            if (!skipEntries.HasValue)
            {
                skipEntries = 0;
            }

            if (!takeEntries.HasValue)
            {
                takeEntries = 20;
            }

            var response = await ((IMapsApi)this).GetMapVotes(gameType, mapId, skipEntries.Value, takeEntries.Value, order, cancellationToken).ConfigureAwait(false);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of map votes with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="mapId">Optional filter by specific map.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of map votes.</returns>
        async Task<ApiResult<CollectionModel<MapVoteDto>>> IMapsApi.GetMapVotes(GameType? gameType, Guid? mapId, int skipEntries, int takeEntries, MapVotesOrder? order, CancellationToken cancellationToken)
        {
            var baseQuery = context.MapVotes.AsNoTracking().AsQueryable();

            var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply filters (without Include for efficient counting)
            if (gameType.HasValue)
            {
                var compatibleGameTypes = gameType.Value.GetCompatibleMapGameTypes().Select(game => (int)game).ToArray();
                baseQuery = baseQuery.Where(mv => compatibleGameTypes.Contains(mv.Map.GameType));
            }

            if (mapId.HasValue)
            {
                var selectedMaps = context.Maps.Where(map => map.MapId == mapId.Value);
                baseQuery = baseQuery.Where(vote => selectedMaps.Any(map =>
                    map.MapId == vote.MapId ||
                    ((map.GameType == (int)GameType.CallOfDuty4 || map.GameType == (int)GameType.CallOfDuty4x) &&
                     (vote.Map.GameType == (int)GameType.CallOfDuty4 || vote.Map.GameType == (int)GameType.CallOfDuty4x) &&
                     map.MapName == vote.Map.MapName)));
            }

            var filteredCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply ordering with stable tiebreaker
            var orderedQuery = order switch
            {
                MapVotesOrder.TimestampAsc => baseQuery.OrderBy(mv => mv.Timestamp).ThenBy(mv => mv.MapVoteId),
                MapVotesOrder.MapNameAsc => baseQuery.OrderBy(mv => mv.Map.MapName).ThenBy(mv => mv.MapVoteId),
                MapVotesOrder.MapNameDesc => baseQuery.OrderByDescending(mv => mv.Map.MapName).ThenBy(mv => mv.MapVoteId),
                _ => baseQuery.OrderByDescending(mv => mv.Timestamp).ThenBy(mv => mv.MapVoteId)
            };

            // Include navigation properties only for the final paged query
            var pagedQuery = orderedQuery
                .Include(mv => mv.Map)
                .Include(mv => mv.Player)
                .Include(mv => mv.GameServer)
                .Skip(skipEntries)
                .Take(takeEntries);

            var results = await pagedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = results.Select(mv => mv.ToDto()).ToList();

            var data = new CollectionModel<MapVoteDto>(entries);

            return new ApiResponse<CollectionModel<MapVoteDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Rebuilds the popularity statistics for all maps based on vote data.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response indicating the map popularity was rebuilt.</returns>
        [HttpPost("maps/popularity")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RebuildMapPopularity(CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).RebuildMapPopularity(cancellationToken).ConfigureAwait(false);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Rebuilds the popularity statistics for all maps based on vote data.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map popularity was rebuilt.</returns>
        async Task<ApiResult> IMapsApi.RebuildMapPopularity(CancellationToken cancellationToken)
        {
            // Use a single SQL statement to compute and update aggregates server-side
            // instead of loading all Maps + MapVotes into memory.
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE m SET
                    m.[TotalLikes] = ISNULL(v.Likes, 0),
                    m.[TotalDislikes] = ISNULL(v.Dislikes, 0),
                    m.[TotalVotes] = ISNULL(v.Total, 0),
                    m.[LikePercentage] = CASE WHEN ISNULL(v.Total, 0) > 0 THEN CAST(ISNULL(v.Likes, 0) AS FLOAT) / v.Total * 100 ELSE 0 END,
                    m.[DislikePercentage] = CASE WHEN ISNULL(v.Total, 0) > 0 THEN CAST(ISNULL(v.Dislikes, 0) AS FLOAT) / v.Total * 100 ELSE 0 END
                FROM [dbo].[Maps] m
                LEFT JOIN (
                    SELECT [MapId],
                           SUM(CASE WHEN [Like] = 1 THEN 1 ELSE 0 END) AS Likes,
                           SUM(CASE WHEN [Like] = 0 THEN 1 ELSE 0 END) AS Dislikes,
                           COUNT(*) AS Total
                    FROM [dbo].[MapVotes]
                    GROUP BY [MapId]
                ) v ON m.[MapId] = v.[MapId]", cancellationToken).ConfigureAwait(false);

            await cacheInvalidator.InvalidateAllMapsAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Creates or updates a map vote for a specific player and server.
        /// </summary>
        /// <param name="upsertMapVoteDto">The map vote data to create or update.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map vote was processed; otherwise, appropriate error responses.</returns>
        [HttpPost("maps/vote")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpsertMapVote([FromBody] UpsertMapVoteDto upsertMapVoteDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).UpsertMapVote(upsertMapVoteDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates or updates a map vote for a specific player and server.
        /// </summary>
        /// <param name="upsertMapVoteDto">The map vote data to create or update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map vote was processed successfully.</returns>
        async Task<ApiResult> IMapsApi.UpsertMapVote(UpsertMapVoteDto upsertMapVoteDto, CancellationToken cancellationToken)
        {
            return await ((IMapsApi)this).UpsertMapVotes([upsertMapVoteDto], cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates or updates multiple map votes in a single operation.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if all map votes were processed; otherwise, appropriate error responses.</returns>
        [HttpPost("maps/votes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpsertMapVotes(CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync().ConfigureAwait(false);

            List<UpsertMapVoteDto>? upsertMapVoteDtos;
            try
            {
                upsertMapVoteDtos = JsonConvert.DeserializeObject<List<UpsertMapVoteDto>>(requestBody);
            }
            catch
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.DeserializationError, ApiErrorMessages.InvalidRequestBodyMessage));
                return err.ToBadRequestResult().ToHttpResult();
            }

            if (upsertMapVoteDtos == null || !upsertMapVoteDtos.Any())
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage));
                return err.ToBadRequestResult().ToHttpResult();
            }

            var response = await ((IMapsApi)this).UpsertMapVotes(upsertMapVoteDtos, cancellationToken).ConfigureAwait(false);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates or updates multiple map votes in a single operation.
        /// </summary>
        /// <param name="upsertMapVoteDtos">The list of map vote data to create or update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating all map votes were processed successfully.</returns>
        async Task<ApiResult> IMapsApi.UpsertMapVotes(List<UpsertMapVoteDto> upsertMapVoteDtos, CancellationToken cancellationToken)
        {
            Guid[] familyMapIds = [];
            var commitAttempted = false;
            var commitUncertain = false;
            ApiResult UnconfirmedCommit()
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.MapVoteCommitUnconfirmedMessage))
                    .ToApiResult(HttpStatusCode.Conflict);
            }
            async Task<ApiResult> UpsertAsync()
            {
                if (commitAttempted)
                {
                    commitUncertain = true;
                    return UnconfirmedCommit();
                }
                context.ChangeTracker.Clear();
                await using var transaction = context.Database.IsRelational()
                    ? await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false) : null;
                var mapIds = upsertMapVoteDtos.Select(x => x.MapId).ToList();
                var requestedMaps = await context.Maps.AsNoTracking().Where(map => mapIds.Contains(map.MapId))
                    .Select(map => new { map.MapId, map.GameType, map.MapName })
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                if (mapIds.Except(requestedMaps.Select(map => map.MapId)).Any())
                {
                    return new ApiResult(HttpStatusCode.NotFound);
                }

                var familyNames = requestedMaps.Where(map => map.GameType == (int)GameType.CallOfDuty4 || map.GameType == (int)GameType.CallOfDuty4x)
                    .Select(map => map.MapName).ToArray();
                var canonicalPairs = await (
                    from map in context.Maps
                    join canonical in context.Maps.Where(map => map.GameType == (int)GameType.CallOfDuty4)
                        on map.MapName equals canonical.MapName
                    where (map.GameType == (int)GameType.CallOfDuty4 || map.GameType == (int)GameType.CallOfDuty4x)
                        && familyNames.Contains(map.MapName)
                    select new { map.MapId, CanonicalId = canonical.MapId })
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                if (canonicalPairs.GroupBy(map => map.MapId).Any(group => group.Count() > 1))
                {
                    return new ApiResult(HttpStatusCode.Conflict);
                }
                var canonicalIds = canonicalPairs.ToDictionary(map => map.MapId, map => map.CanonicalId);
                foreach (var map in requestedMaps.Where(map => map.GameType != (int)GameType.CallOfDuty4 && map.GameType != (int)GameType.CallOfDuty4x))
                {
                    canonicalIds[map.MapId] = map.MapId;
                }
                if (mapIds.Except(canonicalIds.Keys).Any())
                {
                    return new ApiResult(HttpStatusCode.NotFound);
                }

                familyMapIds = canonicalIds.Keys.ToArray();
                if (context.Database.IsSqlServer())
                {
                    var lockKeys = upsertMapVoteDtos.Select(vote =>
                        $"Portal.MapVote:{canonicalIds[vote.MapId]:N}:{vote.PlayerId:N}:{vote.GameServerId:N}")
                        .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
                    var lockKeysJson = JsonConvert.SerializeObject(lockKeys);
                    await context.Database.ExecuteSqlInterpolatedAsync($"""
                        DECLARE @locks table (Position int PRIMARY KEY, Resource nvarchar(255));
                        INSERT @locks SELECT CONVERT(int, [key]), [value] FROM OPENJSON({lockKeysJson});
                        DECLARE @position int = 0, @resource nvarchar(255), @result int;
                        WHILE @position < (SELECT COUNT(*) FROM @locks)
                        BEGIN
                            SELECT @resource = Resource FROM @locks WHERE Position = @position;
                            EXEC @result = sys.sp_getapplock
                                @Resource = @resource,
                                @LockMode = N'Exclusive',
                                @LockOwner = N'Transaction',
                                @LockTimeout = 30000;
                            IF @result < 0 THROW 51000, 'Unable to lock the map vote.', 1;
                            SET @position += 1;
                        END;
                        """, cancellationToken).ConfigureAwait(false);
                }
                var playerIds = upsertMapVoteDtos.Select(x => x.PlayerId).ToList();
                var gameServerIds = upsertMapVoteDtos.Select(x => x.GameServerId).ToList();

                var existingVotes = await context.MapVotes
                    .Where(mv => familyMapIds.Contains(mv.MapId) && playerIds.Contains(mv.PlayerId) &&
                                (mv.GameServerId == null || gameServerIds.Contains(mv.GameServerId.Value)))
                    .ToListAsync(cancellationToken).ConfigureAwait(false);

                foreach (var upsertMapVote in upsertMapVoteDtos)
                {
                    var canonicalId = canonicalIds[upsertMapVote.MapId];
                    var matchingVotes = existingVotes
                        .Where(mv => canonicalIds[mv.MapId] == canonicalId && mv.PlayerId == upsertMapVote.PlayerId && mv.GameServerId == upsertMapVote.GameServerId)
                        .ToList();
                    if (matchingVotes.Select(vote => vote.MapId).Distinct().Skip(1).Any())
                    {
                        return new ApiResult(HttpStatusCode.Conflict);
                    }
                    var mapVote = matchingVotes.FirstOrDefault();

                    // Move the entire existing group without deleting pre-existing duplicate votes.
                    foreach (var existing in matchingVotes)
                    {
                        existing.MapId = canonicalId;
                    }

                    if (mapVote == null)
                    {
                        var newMapVote = upsertMapVote.ToEntity();
                        newMapVote.MapId = canonicalId;
                        context.MapVotes.Add(newMapVote);
                        existingVotes.Add(newMapVote);
                    }
                    else
                    {
                        if (mapVote.Like != upsertMapVote.Like)
                        {
                            mapVote.Like = upsertMapVote.Like;
                            mapVote.Timestamp = DateTime.UtcNow;
                        }
                    }
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                if (transaction != null)
                {
                    commitAttempted = true;
                    try
                    {
                        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        // Replaying an unknown commit could overwrite a newer successful vote.
                        commitUncertain = true;
                        logger.LogWarning(exception, "The map vote commit could not be confirmed.");
                        return UnconfirmedCommit();
                    }
                }
                return new ApiResponse().ToApiResult();
            }

            var result = await context.Database.CreateExecutionStrategy().ExecuteAsync(UpsertAsync).ConfigureAwait(false);
            if (result.IsSuccess || commitUncertain)
            {
                await Task.WhenAll(familyMapIds.Select(id => cacheInvalidator.InvalidateMapAsync(id, cancellationToken))).ConfigureAwait(false);
            }
            return result;
        }

        /// <summary>
        /// Updates a map image by uploading a new image file.
        /// </summary>
        /// <param name="mapId">The unique identifier of the map to update the image for.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map image was updated; otherwise, appropriate error responses.</returns>
        [HttpPost("maps/{mapId:guid}/image")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateMapImage(Guid mapId, CancellationToken cancellationToken = default)
        {
            if (Request.Form.Files.Count == 0)
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.NoFilesProvided, ApiErrorMessages.NoFilesProvidedMessage));
                return err.ToBadRequestResult().ToHttpResult();
            }

            var file = Request.Form.Files.First();

            var filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            var response = await ((IMapsApi)this).UpdateMapImage(mapId, filePath, cancellationToken).ConfigureAwait(false);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates a map image by uploading a new image file.
        /// </summary>
        /// <param name="mapId">The unique identifier of the map to update the image for.</param>
        /// <param name="filePath">The path to the temporary image file to upload.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map image was updated if successful; otherwise, appropriate error responses.</returns>
        async Task<ApiResult> IMapsApi.UpdateMapImage(Guid mapId, string filePath, CancellationToken cancellationToken)
        {
            if (!TryResolveTemporaryFilePath(filePath, out var safeFilePath) || !System.IO.File.Exists(safeFilePath))
            {
                return new ApiResult(HttpStatusCode.BadRequest);
            }

            var map = await context.Maps
                .FirstOrDefaultAsync(m => m.MapId == mapId, cancellationToken).ConfigureAwait(false);

            if (map == null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            var blobEndpoint = Environment.GetEnvironmentVariable("appdata_storage_blob_endpoint");
            if (string.IsNullOrEmpty(blobEndpoint))
            {
                return new ApiResult(HttpStatusCode.InternalServerError);
            }

            var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient("map-images");

            var blobKey = $"{map.GameType.ToGameType()}_{map.MapName}.jpg"; // Stored as .jpg regardless of original extension
            var blobClient = containerClient.GetBlobClient(blobKey);
            if (await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                await blobClient.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Determine content type (default to jpeg as we enforce .jpg suffix)
            var contentType = GetContentTypeFromExtension(Path.GetExtension(safeFilePath));

            await using var fileStream = System.IO.File.OpenRead(safeFilePath);
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = new Dictionary<string, string>
                {
                    ["mapId"] = map.MapId.ToString(),
                    ["gameType"] = map.GameType.ToString(),
                    ["mapName"] = map.MapName ?? string.Empty
                }
            };

            await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken).ConfigureAwait(false);

            map.MapImageUri = blobClient.Uri.ToString();

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await cacheInvalidator.InvalidateMapAsync(mapId, cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Clears (deletes) the current map image if present from blob storage and updates the map entity.
        /// </summary>
        /// <param name="mapId">The unique identifier of the map whose image will be cleared.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map image was cleared (or did not exist); otherwise, appropriate error responses.</returns>
        [HttpDelete("maps/{mapId:guid}/image")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ClearMapImage(Guid mapId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).ClearMapImage(mapId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Clears (deletes) the current map image if present from blob storage and updates the map entity.
        /// </summary>
        /// <param name="mapId">The unique identifier of the map whose image will be cleared.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An ApiResult indicating success or failure.</returns>
        async Task<ApiResult> IMapsApi.ClearMapImage(Guid mapId, CancellationToken cancellationToken)
        {
            var map = await context.Maps
                .FirstOrDefaultAsync(m => m.MapId == mapId, cancellationToken).ConfigureAwait(false);

            if (map == null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            var blobEndpoint = Environment.GetEnvironmentVariable("appdata_storage_blob_endpoint");
            if (string.IsNullOrEmpty(blobEndpoint))
            {
                return new ApiResult(HttpStatusCode.InternalServerError);
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
                var containerClient = blobServiceClient.GetBlobContainerClient("map-images");

                // Determine blob name (same pattern used when uploading)
                var blobKey = $"{map.GameType.ToGameType()}_{map.MapName}.jpg";
                var blobClient = containerClient.GetBlobClient(blobKey);

                if (await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
                {
                    await blobClient.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // Log but swallow storage exceptions so DB reference can still be cleared
                logger.LogWarning(ex, "Failed to delete map image blob for MapId {MapId} while clearing image.", mapId);
            }

            map.MapImageUri = null;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await cacheInvalidator.InvalidateMapAsync(mapId, cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Applies filtering criteria to the map query.
        /// </summary>
        /// <param name="query">The base query to filter.</param>
        /// <param name="gameType">Optional game type filter.</param>
        /// <param name="mapNames">Optional map names filter.</param>
        /// <param name="filter">Optional maps filter.</param>
        /// <param name="filterString">Optional string filter for map names.</param>
        /// <returns>The filtered query.</returns>
        private IQueryable<Map> ApplyFilter(IQueryable<Map> query, GameType? gameType, string[]? mapNames, MapsFilter? filter, string? filterString)
        {
            query = query.Where(m => m.GameType != (int)GameType.CallOfDuty4x);
            if (gameType.HasValue)
            {
                var mapGameType = gameType.Value.GetMapGameType().ToGameTypeInt();
                query = query.Where(m => m.GameType == mapGameType);
            }

            if (mapNames?.Length > 0)
            {
                query = query.Where(m => mapNames.Contains(m.MapName)).AsQueryable();
            }

            if (!string.IsNullOrEmpty(filterString))
            {
                query = query.Where(m => m.MapName != null && m.MapName.Contains(filterString));
            }

            query = filter switch
            {
                MapsFilter.EmptyMapImage => query.Where(m => m.MapImageUri == null),
                _ => query
            };

            return query;
        }

        /// <summary>
        /// Applies ordering and pagination to the map query.
        /// </summary>
        /// <param name="query">The base query to order and paginate.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria.</param>
        /// <returns>The ordered and paginated query.</returns>
        private IQueryable<Map> ApplyOrderAndLimits(IQueryable<Map> query, int skipEntries, int takeEntries, MapsOrder? order)
        {
            var orderedQuery = order switch
            {
                MapsOrder.MapNameAsc => query.OrderBy(m => m.MapName),
                MapsOrder.MapNameDesc => query.OrderByDescending(m => m.MapName),
                MapsOrder.GameTypeAsc => query.OrderBy(m => m.GameType),
                MapsOrder.GameTypeDesc => query.OrderByDescending(m => m.GameType),
                MapsOrder.PopularityAsc => query.OrderBy(m => m.TotalLikes),
                MapsOrder.PopularityDesc => query.OrderByDescending(m => m.TotalLikes),
                _ => query.OrderBy(m => m.MapName)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }

        private static bool TryResolveTemporaryFilePath(string filePath, out string safeFilePath)
        {
            safeFilePath = string.Empty;
            if (string.IsNullOrWhiteSpace(filePath) || !Path.IsPathFullyQualified(filePath))
            {
                return false;
            }

            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            safeFilePath = Path.Combine(Path.GetTempPath(), fileName);
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return string.Equals(
                Path.GetFullPath(filePath),
                Path.GetFullPath(safeFilePath),
                comparison);
        }

        /// <summary>
        /// Attempts to infer the MIME content type from a file extension.
        /// Falls back to image/jpeg (expected type) then application/octet-stream.
        /// </summary>
        /// <param name="extension">File extension including leading dot.</param>
        /// <returns>Resolved MIME type string.</returns>
        private static string GetContentTypeFromExtension(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return "image/jpeg"; // default expected type
            }

            return extension.ToLowerInvariant() switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
