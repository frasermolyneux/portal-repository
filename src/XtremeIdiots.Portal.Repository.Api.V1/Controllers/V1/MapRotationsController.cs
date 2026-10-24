using System.Data;
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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("v{version:apiVersion}")]
    public class MapRotationsController : ControllerBase, IMapRotationsApi
    {
        private readonly PortalDbContext context;

        public MapRotationsController(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        // ───────────────────────── Map Rotation CRUD ─────────────────────────

        /// <summary>
        /// Retrieves a specific map rotation by its unique identifier.
        /// </summary>
        /// <param name="mapRotationId">The unique identifier of the map rotation to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The map rotation details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("map-rotations/{mapRotationId:guid}")]
        [ProducesResponseType<MapRotationDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMapRotation(Guid mapRotationId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapRotationsApi)this).GetMapRotation(mapRotationId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific map rotation by its unique identifier.
        /// </summary>
        /// <param name="mapRotationId">The unique identifier of the map rotation to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the map rotation details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<MapRotationDto>> IMapRotationsApi.GetMapRotation(Guid mapRotationId, CancellationToken cancellationToken)
        {
            var entity = await context.MapRotations
                .Include(mr => mr.MapRotationMaps)
                .Include(mr => mr.MapRotationServerAssignments)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(mr => mr.MapRotationId == mapRotationId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                return new ApiResult<MapRotationDto>(HttpStatusCode.NotFound);
            }

            var result = entity.ToDto();
            return new ApiResponse<MapRotationDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a paginated list of map rotations with optional filtering and sorting.
        /// </summary>
        /// <param name="gameTypes">Optional comma-separated list of game types to filter by.</param>
        /// <param name="gameMode">Optional game mode to filter by.</param>
        /// <param name="filter">Optional filter criteria for map rotations.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of map rotations.</returns>
        [HttpGet("map-rotations")]
        [ProducesResponseType<CollectionModel<MapRotationDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMapRotations(
            [FromQuery] string? gameTypes = null,
            [FromQuery] string? gameMode = null,
            [FromQuery] MapRotationStatus? status = null,
            [FromQuery] string? filterString = null,
            [FromQuery] Guid? createdByUserId = null,
            [FromQuery] MapRotationsFilter? filter = null,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            [FromQuery] MapRotationsOrder? order = null,
            CancellationToken cancellationToken = default)
        {
            GameType[]? gameTypesFilter = null;
            if (!string.IsNullOrWhiteSpace(gameTypes))
            {
                var split = gameTypes.Split(",");
                var parsed = new List<GameType>();
                foreach (var gt in split)
                {
                    if (!Enum.TryParse<GameType>(gt.Trim(), out var gameType) || !Enum.IsDefined(gameType))
                    {
                        return new ApiResponse(new ApiError("INVALID_GAME_TYPE", $"Invalid game type value: '{gt.Trim()}'."))
                            .ToBadRequestResult()
                            .ToHttpResult();
                    }

                    parsed.Add(gameType);
                }
                gameTypesFilter = parsed.ToArray();
            }

            var response = await ((IMapRotationsApi)this).GetMapRotations(gameTypesFilter, gameMode, status, filterString, createdByUserId, filter, skipEntries, takeEntries, order, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of map rotations with optional filtering and sorting.
        /// </summary>
        /// <param name="gameTypes">Optional array of game types to filter by.</param>
        /// <param name="gameMode">Optional game mode to filter by.</param>
        /// <param name="filter">Optional filter criteria for map rotations.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of map rotations.</returns>
        async Task<ApiResult<CollectionModel<MapRotationDto>>> IMapRotationsApi.GetMapRotations(
            GameType[]? gameTypes,
            string? gameMode,
            MapRotationsFilter? filter,
            int skipEntries,
            int takeEntries,
            MapRotationsOrder? order,
            CancellationToken cancellationToken)
        {
            return await ((IMapRotationsApi)this).GetMapRotations(gameTypes, gameMode, null, null, null, filter, skipEntries, takeEntries, order, cancellationToken).ConfigureAwait(false);
        }

        async Task<ApiResult<CollectionModel<MapRotationDto>>> IMapRotationsApi.GetMapRotations(
            GameType[]? gameTypes,
            string? gameMode,
            MapRotationStatus? status,
            string? filterString,
            MapRotationsFilter? filter,
            int skipEntries,
            int takeEntries,
            MapRotationsOrder? order,
            CancellationToken cancellationToken)
        {
            return await ((IMapRotationsApi)this).GetMapRotations(gameTypes, gameMode, status, filterString, null, filter, skipEntries, takeEntries, order, cancellationToken).ConfigureAwait(false);
        }

        async Task<ApiResult<CollectionModel<MapRotationDto>>> IMapRotationsApi.GetMapRotations(
            GameType[]? gameTypes,
            string? gameMode,
            MapRotationStatus? status,
            string? filterString,
            Guid? createdByUserId,
            MapRotationsFilter? filter,
            int skipEntries,
            int takeEntries,
            MapRotationsOrder? order,
            CancellationToken cancellationToken)
        {
            var baseQuery = context.MapRotations.AsNoTracking();

            var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var filteredQuery = ApplyFilters(baseQuery, gameTypes, gameMode, status, filterString, createdByUserId, filter);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var orderedQuery = ApplyOrderingAndPagination(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery
                .Include(mr => mr.MapRotationMaps)
                .Include(mr => mr.MapRotationServerAssignments)
                .Include(mr => mr.CreatedByUser)
                .Include(mr => mr.LastModifiedByUser)
                .AsSplitQuery()
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = results.Select(m => m.ToDto()).ToList();

            var data = new CollectionModel<MapRotationDto>(entries);

            return new ApiResponse<CollectionModel<MapRotationDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Creates a new map rotation.
        /// </summary>
        /// <param name="createMapRotationDto">The map rotation data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The created map rotation if successful; otherwise, a 400 Bad Request response.</returns>
        [HttpPost("map-rotations")]
        [ProducesResponseType<MapRotationDto>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMapRotation([FromBody] CreateMapRotationDto createMapRotationDto, CancellationToken cancellationToken = default)
        {
            if (createMapRotationDto == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            var response = await ((IMapRotationsApi)this).CreateMapRotation(createMapRotationDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new map rotation.
        /// </summary>
        /// <param name="createMapRotationDto">The map rotation data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the created map rotation.</returns>
        async Task<ApiResult<MapRotationDto>> IMapRotationsApi.CreateMapRotation(CreateMapRotationDto createMapRotationDto, CancellationToken cancellationToken)
        {
            var entity = createMapRotationDto.ToEntity();

            var mapIds = createMapRotationDto.MapIds ?? [];
            if (mapIds.Count > 0)
            {
                var distinctMapIds = mapIds.Distinct().ToList();
                var maps = await context.Maps
                    .AsNoTracking()
                    .Where(m => distinctMapIds.Contains(m.MapId))
                    .ToListAsync(cancellationToken).ConfigureAwait(false);

                if (maps.Count != distinctMapIds.Count)
                {
                    return new ApiResult<MapRotationDto>(HttpStatusCode.BadRequest,
                        new ApiResponse<MapRotationDto>(new ApiError("INVALID_MAP_IDS", "One or more map IDs do not exist.")));
                }

                var gameTypeInt = (int)createMapRotationDto.GameType;
                if (maps.Any(m => m.GameType != gameTypeInt))
                {
                    return new ApiResult<MapRotationDto>(HttpStatusCode.BadRequest,
                        new ApiResponse<MapRotationDto>(new ApiError("MAP_GAME_TYPE_MISMATCH", "One or more maps do not match the rotation's game type.")));
                }

                entity.ContentHash = MapRotationsMappingExtensions.ComputeContentHash(distinctMapIds);
                entity.MapRotationMaps = distinctMapIds.Select((mapId, index) => new MapRotationMap
                {
                    MapId = mapId,
                    SortOrder = index
                }).ToList();
            }

            context.MapRotations.Add(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var created = await context.MapRotations
                .Include(mr => mr.MapRotationMaps)
                .Include(mr => mr.MapRotationServerAssignments)
                .AsNoTracking()
                .FirstAsync(mr => mr.MapRotationId == entity.MapRotationId, cancellationToken).ConfigureAwait(false);

            var result = created.ToDto();
            return new ApiResponse<MapRotationDto>(result).ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Updates an existing map rotation.
        /// </summary>
        /// <param name="mapRotationId">The unique identifier of the map rotation to update.</param>
        /// <param name="updateMapRotationDto">The map rotation data to update.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map rotation was updated; otherwise, a 404 Not Found response.</returns>
        [HttpPatch("map-rotations/{mapRotationId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMapRotation(Guid mapRotationId, [FromBody] UpdateMapRotationDto updateMapRotationDto, CancellationToken cancellationToken = default)
        {
            if (updateMapRotationDto == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            if (updateMapRotationDto.MapRotationId != mapRotationId)
            {
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestEntityMismatch, ApiErrorMessages.RequestEntityMismatchMessage))).ToHttpResult();
            }

            var response = await ((IMapRotationsApi)this).UpdateMapRotation(updateMapRotationDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates an existing map rotation.
        /// </summary>
        /// <param name="updateMapRotationDto">The map rotation data to update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map rotation was updated if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapRotationsApi.UpdateMapRotation(UpdateMapRotationDto updateMapRotationDto, CancellationToken cancellationToken)
            => await ExecuteMutationAsync(() => UpdateMapRotation(updateMapRotationDto, cancellationToken), cancellationToken).ConfigureAwait(false);

        private async Task<ApiResult> UpdateMapRotation(UpdateMapRotationDto updateMapRotationDto, CancellationToken cancellationToken)
        {
            var entity = await context.MapRotations
                .Include(mr => mr.MapRotationMaps)
                .FirstOrDefaultAsync(mr => mr.MapRotationId == updateMapRotationDto.MapRotationId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            var contentChanged = updateMapRotationDto.GameMode is not null
                && updateMapRotationDto.GameMode != entity.GameMode;
            updateMapRotationDto.ApplyTo(entity);

            if (updateMapRotationDto.MapIds != null)
            {
                var distinctMapIds = updateMapRotationDto.MapIds.Distinct().ToList();
                var maps = await context.Maps
                    .AsNoTracking()
                    .Where(m => distinctMapIds.Contains(m.MapId))
                    .ToListAsync(cancellationToken).ConfigureAwait(false);

                if (maps.Count != distinctMapIds.Count)
                {
                    return new ApiResult(HttpStatusCode.BadRequest,
                        new ApiResponse(new ApiError("INVALID_MAP_IDS", "One or more map IDs do not exist.")));
                }

                if (maps.Any(m => m.GameType != entity.GameType))
                {
                    return new ApiResult(HttpStatusCode.BadRequest,
                        new ApiResponse(new ApiError("MAP_GAME_TYPE_MISMATCH", "One or more maps do not match the rotation's game type.")));
                }

                var newHash = MapRotationsMappingExtensions.ComputeContentHash(distinctMapIds);
                if (newHash != entity.ContentHash)
                {
                    context.MapRotationMaps.RemoveRange(entity.MapRotationMaps);

                    entity.MapRotationMaps = distinctMapIds.Select((mapId, index) => new MapRotationMap
                    {
                        MapRotationId = entity.MapRotationId,
                        MapId = mapId,
                        SortOrder = index
                    }).ToList();

                    entity.ContentHash = newHash;
                    contentChanged = true;
                }
            }

            if (contentChanged)
            {
                entity.Version++;
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Deletes a map rotation by its unique identifier.
        /// </summary>
        /// <param name="mapRotationId">The unique identifier of the map rotation to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map rotation was deleted; otherwise, a 404 Not Found response.</returns>
        [HttpDelete("map-rotations/{mapRotationId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMapRotation(Guid mapRotationId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapRotationsApi)this).DeleteMapRotation(mapRotationId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Deletes a map rotation by its unique identifier.
        /// </summary>
        /// <param name="mapRotationId">The unique identifier of the map rotation to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map rotation was deleted if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapRotationsApi.DeleteMapRotation(Guid mapRotationId, CancellationToken cancellationToken)
        {
            var deleted = false;
            return await ExecuteMutationAsync(async () =>
            {
                var result = await DeleteRotation(mapRotationId, deleted, cancellationToken).ConfigureAwait(false);
                deleted |= result.StatusCode == HttpStatusCode.OK;
                return result;
            }, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ApiResult> DeleteRotation(Guid mapRotationId, bool alreadyDeleted, CancellationToken cancellationToken)
        {
            var entity = await context.MapRotations
                .Include(mr => mr.MapRotationMaps)
                .Include(mr => mr.MapRotationServerAssignments)
                    .ThenInclude(a => a.MapRotationAssignmentOperations)
                .FirstOrDefaultAsync(mr => mr.MapRotationId == mapRotationId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                return alreadyDeleted ? new ApiResponse().ToApiResult() : new ApiResult(HttpStatusCode.NotFound);
            }

            if (entity.MapRotationServerAssignments.Any(a =>
                a.DeploymentState != (int)DeploymentState.Removed || !CanRemove(a)
                || RetainUntil(a) > DateTime.UtcNow))
            {
                return new ApiResult(HttpStatusCode.BadRequest,
                    new ApiResponse(new ApiError("HAS_ACTIVE_ASSIGNMENTS", "Unassign all servers and wait for the 90-day history retention period before deleting the rotation.")));
            }

            // Remove children explicitly to avoid FK violations
            foreach (var assignment in entity.MapRotationServerAssignments)
            {
                context.MapRotationAssignmentOperations.RemoveRange(assignment.MapRotationAssignmentOperations);
            }

            context.MapRotationServerAssignments.RemoveRange(entity.MapRotationServerAssignments);
            context.MapRotationMaps.RemoveRange(entity.MapRotationMaps);
            context.MapRotations.Remove(entity);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult();
        }

        // ───────────────────────── Server Assignment CRUD ─────────────────────────

        /// <summary>
        /// Retrieves a specific server assignment by its unique identifier.
        /// </summary>
        /// <param name="assignmentId">The unique identifier of the server assignment to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The server assignment details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("map-rotations/assignments/{assignmentId:guid}")]
        [ProducesResponseType<MapRotationServerAssignmentDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetServerAssignment(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapRotationsApi)this).GetServerAssignment(assignmentId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific server assignment by its unique identifier.
        /// </summary>
        /// <param name="assignmentId">The unique identifier of the server assignment to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the server assignment details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<MapRotationServerAssignmentDto>> IMapRotationsApi.GetServerAssignment(Guid assignmentId, CancellationToken cancellationToken)
        {
            var entity = await context.MapRotationServerAssignments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.MapRotationServerAssignmentId == assignmentId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                return new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.NotFound);
            }

            var result = entity.ToDto();
            return new ApiResponse<MapRotationServerAssignmentDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a paginated list of server assignments with optional filtering.
        /// </summary>
        /// <param name="mapRotationId">Optional map rotation ID to filter by.</param>
        /// <param name="gameServerId">Optional game server ID to filter by.</param>
        /// <param name="deploymentState">Optional deployment state to filter by.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of server assignments.</returns>
        [HttpGet("map-rotations/assignments")]
        [ProducesResponseType<CollectionModel<MapRotationServerAssignmentDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetServerAssignments(
            [FromQuery] Guid? mapRotationId = null,
            [FromQuery] Guid? gameServerId = null,
            [FromQuery] DeploymentState? deploymentState = null,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            CancellationToken cancellationToken = default)
        {
            var response = await ((IMapRotationsApi)this).GetServerAssignments(mapRotationId, gameServerId, deploymentState, skipEntries, takeEntries, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of server assignments with optional filtering.
        /// </summary>
        /// <param name="mapRotationId">Optional map rotation ID to filter by.</param>
        /// <param name="gameServerId">Optional game server ID to filter by.</param>
        /// <param name="deploymentState">Optional deployment state to filter by.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of server assignments.</returns>
        async Task<ApiResult<CollectionModel<MapRotationServerAssignmentDto>>> IMapRotationsApi.GetServerAssignments(
            Guid? mapRotationId,
            Guid? gameServerId,
            DeploymentState? deploymentState,
            int skipEntries,
            int takeEntries,
            CancellationToken cancellationToken)
        {
            var query = context.MapRotationServerAssignments.AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            if (mapRotationId.HasValue)
            {
                query = query.Where(a => a.MapRotationId == mapRotationId.Value);
            }

            if (gameServerId.HasValue)
            {
                query = query.Where(a => a.GameServerId == gameServerId.Value);
            }

            if (deploymentState.HasValue)
            {
                query = query.Where(a => a.DeploymentState == (int)deploymentState.Value);
            }

            var filteredCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var results = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(skipEntries)
                .Take(takeEntries)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = results.Select(a => a.ToDto()).ToList();

            var data = new CollectionModel<MapRotationServerAssignmentDto>(entries);

            return new ApiResponse<CollectionModel<MapRotationServerAssignmentDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Creates a new server assignment for a map rotation.
        /// </summary>
        /// <param name="createDto">The server assignment data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The created server assignment if successful; otherwise, a 400 Bad Request response.</returns>
        [HttpPost("map-rotations/assignments")]
        [ProducesResponseType<MapRotationServerAssignmentDto>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateServerAssignment([FromBody] CreateMapRotationServerAssignmentDto createDto, CancellationToken cancellationToken = default)
        {
            if (createDto == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            var response = await ((IMapRotationsApi)this).CreateServerAssignment(createDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new server assignment for a map rotation.
        /// </summary>
        /// <param name="createDto">The server assignment data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the created server assignment.</returns>
        async Task<ApiResult<MapRotationServerAssignmentDto>> IMapRotationsApi.CreateServerAssignment(CreateMapRotationServerAssignmentDto createDto, CancellationToken cancellationToken)
        {
            var assignmentId = Guid.NewGuid();
            return await ExecuteMutationAsync(() => CreateAssignment(createDto, assignmentId, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private async Task<ApiResult<MapRotationServerAssignmentDto>> CreateAssignment(CreateMapRotationServerAssignmentDto createDto, Guid assignmentId, CancellationToken cancellationToken)
        {
            var committed = await context.MapRotationServerAssignments.AsNoTracking()
                .FirstOrDefaultAsync(a => a.MapRotationServerAssignmentId == assignmentId, cancellationToken).ConfigureAwait(false);
            if (committed != null)
            {
                return new ApiResponse<MapRotationServerAssignmentDto>(committed.ToDto()).ToApiResult(HttpStatusCode.Created);
            }

            var mapRotation = await context.MapRotations
                .AsNoTracking()
                .FirstOrDefaultAsync(mr => mr.MapRotationId == createDto.MapRotationId, cancellationToken).ConfigureAwait(false);

            if (mapRotation == null)
            {
                return new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("MAP_ROTATION_NOT_FOUND", "The specified map rotation does not exist.")));
            }

            var gameServer = await context.GameServers
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.GameServerId == createDto.GameServerId, cancellationToken).ConfigureAwait(false);

            if (gameServer == null)
            {
                return new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("GAME_SERVER_NOT_FOUND", "The specified game server does not exist.")));
            }

            if (mapRotation.GameType != gameServer.GameType)
            {
                return new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("GAME_TYPE_MISMATCH", "The map rotation game type does not match the game server game type.")));
            }

            var duplicateExists = await context.MapRotationServerAssignments
                .AnyAsync(a =>
                    a.MapRotationId == createDto.MapRotationId &&
                    a.GameServerId == createDto.GameServerId &&
                    a.ConfigFilePath == createDto.ConfigFilePath &&
                    a.ConfigVariableName == createDto.ConfigVariableName &&
                    a.DeploymentState != (int)DeploymentState.Removed &&
                    a.DeploymentState != (int)DeploymentState.Removing &&
                    a.DeploymentState != (int)DeploymentState.Failed,
                    cancellationToken).ConfigureAwait(false);

            if (duplicateExists)
            {
                return new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("DUPLICATE_CONFIG_TARGET", "An assignment already exists for this rotation and server with the same config file path and variable name.")));
            }

            var entity = createDto.ToEntity();
            entity.MapRotationServerAssignmentId = assignmentId;
            context.MapRotationServerAssignments.Add(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var created = await context.MapRotationServerAssignments
                .AsNoTracking()
                .FirstAsync(a => a.MapRotationServerAssignmentId == entity.MapRotationServerAssignmentId, cancellationToken).ConfigureAwait(false);

            var result = created.ToDto();
            return new ApiResponse<MapRotationServerAssignmentDto>(result).ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Updates an existing server assignment.
        /// </summary>
        /// <param name="assignmentId">The unique identifier of the server assignment to update.</param>
        /// <param name="updateDto">The server assignment data to update.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the server assignment was updated; otherwise, a 404 Not Found response.</returns>
        [HttpPatch("map-rotations/assignments/{assignmentId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateServerAssignment(Guid assignmentId, [FromBody] UpdateMapRotationServerAssignmentDto updateDto, CancellationToken cancellationToken = default)
        {
            if (updateDto == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            if (updateDto.MapRotationServerAssignmentId != assignmentId)
            {
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestEntityMismatch, ApiErrorMessages.RequestEntityMismatchMessage))).ToHttpResult();
            }

            var response = await ((IMapRotationsApi)this).UpdateServerAssignment(updateDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates an existing server assignment.
        /// </summary>
        /// <param name="updateDto">The server assignment data to update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the server assignment was updated if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapRotationsApi.UpdateServerAssignment(UpdateMapRotationServerAssignmentDto updateDto, CancellationToken cancellationToken)
        {
            var attempt = new AssignmentRemovalAttempt();
            return await ExecuteMutationAsync(() => UpdateAssignment(updateDto, attempt, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private async Task<ApiResult> UpdateAssignment(UpdateMapRotationServerAssignmentDto updateDto, AssignmentRemovalAttempt attempt, CancellationToken cancellationToken)
        {
            var entity = await context.MapRotationServerAssignments
                .Include(a => a.MapRotation)
                .Include(a => a.MapRotationAssignmentOperations)
                .FirstOrDefaultAsync(a => a.MapRotationServerAssignmentId == updateDto.MapRotationServerAssignmentId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            if (attempt.Unassigned && entity.DeploymentState == (int)DeploymentState.Removed
                && entity.UnassignedAt == attempt.RemovedAt)
            {
                return new ApiResponse().ToApiResult();
            }

            if (updateDto.ActivationState == ActivationState.Active
                && entity.DeploymentState == (int)DeploymentState.Removed)
            {
                return new ApiResult(HttpStatusCode.BadRequest,
                    new ApiResponse(new ApiError("ASSIGNMENT_REMOVED", "A removed assignment cannot be activated.")));
            }

            var effectiveConfigFilePath = updateDto.ConfigFilePath ?? entity.ConfigFilePath;
            var effectiveConfigVariableName = updateDto.ConfigVariableName ?? entity.ConfigVariableName;
            var targetChanged = effectiveConfigFilePath != entity.ConfigFilePath
                || effectiveConfigVariableName != entity.ConfigVariableName;
            var effectiveDeploymentState = updateDto.DeploymentState ?? (DeploymentState)entity.DeploymentState;
            var validationError = await ValidateConfigTarget(entity, updateDto, targetChanged, cancellationToken).ConfigureAwait(false)
                ?? ValidateStateValues(updateDto)
                ?? ValidateDeploymentEvidence(entity, updateDto)
                ?? ValidateRemoval(entity, updateDto, targetChanged);
            if (validationError != null)
            {
                return validationError;
            }

            if (await HasDuplicateConfigTarget(entity, effectiveConfigFilePath, effectiveConfigVariableName, effectiveDeploymentState, cancellationToken).ConfigureAwait(false))
            {
                return InvalidAssignment("DUPLICATE_CONFIG_TARGET", "An assignment already exists for this rotation and server with the same config file path and variable name.");
            }

            if (updateDto.ActivationState == ActivationState.Active)
            {
                await ConfirmConfigTargetHandover(entity, effectiveConfigFilePath, effectiveConfigVariableName, cancellationToken).ConfigureAwait(false);
            }

            var wasRemoved = entity.DeploymentState == (int)DeploymentState.Removed;
            updateDto.ApplyTo(entity);
            if (targetChanged)
            {
                entity.ActivatedVersion = null;
            }

            if (effectiveDeploymentState == DeploymentState.Removed && !wasRemoved)
            {
                entity.UnassignedAt = attempt.RemovedAt;
            }
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            attempt.Unassigned = effectiveDeploymentState == DeploymentState.Removed && !wasRemoved;
            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Unassigns an inactive server assignment without deleting map files, or purges retained history after 90 days.
        /// </summary>
        /// <param name="assignmentId">The unique identifier of the server assignment to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the server assignment was marked for removal; otherwise, a 404 Not Found response.</returns>
        [HttpDelete("map-rotations/assignments/{assignmentId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteServerAssignment(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapRotationsApi)this).DeleteServerAssignment(assignmentId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Unassigns an inactive server assignment without deleting map files, or purges retained history after 90 days.
        /// </summary>
        /// <param name="assignmentId">The unique identifier of the server assignment to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the server assignment was marked for removal if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapRotationsApi.DeleteServerAssignment(Guid assignmentId, CancellationToken cancellationToken)
        {
            var attempt = new AssignmentRemovalAttempt();
            return await ExecuteMutationAsync(() => DeleteAssignment(assignmentId, attempt, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private async Task<ApiResult> DeleteAssignment(Guid assignmentId, AssignmentRemovalAttempt attempt, CancellationToken cancellationToken)
        {
            var entity = await context.MapRotationServerAssignments
                .Include(a => a.MapRotationAssignmentOperations)
                .FirstOrDefaultAsync(a => a.MapRotationServerAssignmentId == assignmentId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                return attempt.Purged ? new ApiResponse().ToApiResult() : new ApiResult(HttpStatusCode.NotFound);
            }

            if (attempt.Unassigned && entity.DeploymentState == (int)DeploymentState.Removed
                && entity.UnassignedAt == attempt.RemovedAt)
            {
                return new ApiResponse().ToApiResult();
            }

            if (!CanRemove(entity))
            {
                return InvalidAssignment("ASSIGNMENT_IN_USE", "Deactivate the assignment and finish outstanding operations before unassigning.");
            }

            if (entity.DeploymentState == (int)DeploymentState.Removed)
            {
                var purgeAt = RetainUntil(entity);

                if (DateTime.UtcNow < purgeAt)
                {
                    var retentionMessage = $"Removed assignments are retained for 90 days and cannot be deleted before {purgeAt:O}.";

                    return new ApiResult(HttpStatusCode.BadRequest,
                        new ApiResponse(new ApiError("ASSIGNMENT_RETENTION_ACTIVE", retentionMessage)));
                }

                if (entity.MapRotationAssignmentOperations.Count != 0)
                {
                    context.MapRotationAssignmentOperations.RemoveRange(entity.MapRotationAssignmentOperations);
                }

                context.MapRotationServerAssignments.Remove(entity);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                attempt.Purged = true;
                return new ApiResponse().ToApiResult();
            }

            // Unassignment is metadata-only: shared map files are never scheduled for deletion.
            entity.DeploymentState = (int)DeploymentState.Removed;
            entity.UnassignedAt = attempt.RemovedAt;
            entity.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            attempt.Unassigned = true;
            return new ApiResponse().ToApiResult();
        }

        // ───────────────────────── Assignment Operations ─────────────────────────

        /// <summary>
        /// Retrieves assignment operations for a specific server assignment.
        /// </summary>
        /// <param name="assignmentId">The unique identifier of the server assignment.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of assignment operations.</returns>
        [HttpGet("map-rotations/assignments/{assignmentId:guid}/operations")]
        [ProducesResponseType<CollectionModel<MapRotationAssignmentOperationDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignmentOperations(Guid assignmentId, [FromQuery] int skipEntries = 0, [FromQuery] int takeEntries = 20, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapRotationsApi)this).GetAssignmentOperations(assignmentId, skipEntries, takeEntries, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves assignment operations for a specific server assignment.
        /// </summary>
        /// <param name="assignmentId">The unique identifier of the server assignment.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of assignment operations.</returns>
        async Task<ApiResult<CollectionModel<MapRotationAssignmentOperationDto>>> IMapRotationsApi.GetAssignmentOperations(
            Guid assignmentId,
            int skipEntries,
            int takeEntries,
            CancellationToken cancellationToken)
        {
            var query = context.MapRotationAssignmentOperations
                .AsNoTracking()
                .Where(op => op.MapRotationServerAssignmentId == assignmentId);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var results = await query
                .OrderByDescending(op => op.StartedAt)
                .Skip(skipEntries)
                .Take(takeEntries)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = results.Select(op => op.ToDto()).ToList();

            var data = new CollectionModel<MapRotationAssignmentOperationDto>(entries);

            return new ApiResponse<CollectionModel<MapRotationAssignmentOperationDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, totalCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Creates a new assignment operation.
        /// </summary>
        /// <param name="createDto">The assignment operation data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The created assignment operation if successful; otherwise, a 400 Bad Request response.</returns>
        [HttpPost("map-rotations/assignments/operations")]
        [ProducesResponseType<MapRotationAssignmentOperationDto>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAssignmentOperation([FromBody] CreateMapRotationAssignmentOperationDto createDto, CancellationToken cancellationToken = default)
        {
            if (createDto == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            var response = await ((IMapRotationsApi)this).CreateAssignmentOperation(createDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new assignment operation.
        /// </summary>
        /// <param name="createDto">The assignment operation data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the created assignment operation.</returns>
        async Task<ApiResult<MapRotationAssignmentOperationDto>> IMapRotationsApi.CreateAssignmentOperation(CreateMapRotationAssignmentOperationDto createDto, CancellationToken cancellationToken)
        {
            var operationId = Guid.NewGuid();
            return await ExecuteMutationAsync(() => CreateOperation(createDto, operationId, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private async Task<ApiResult<MapRotationAssignmentOperationDto>> CreateOperation(CreateMapRotationAssignmentOperationDto createDto, Guid operationId, CancellationToken cancellationToken)
        {
            // The commit may have succeeded even if its acknowledgement was lost.
            var committed = await context.MapRotationAssignmentOperations.AsNoTracking()
                .Where(op => op.MapRotationAssignmentOperationId == operationId
                    || (!string.IsNullOrEmpty(createDto.DurableFunctionInstanceId)
                        && op.MapRotationServerAssignmentId == createDto.MapRotationServerAssignmentId
                        && op.OperationType == (int)createDto.OperationType
                        && op.Status == (int)AssignmentOperationStatus.InProgress
                        && op.DurableFunctionInstanceId == createDto.DurableFunctionInstanceId))
                .OrderByDescending(op => op.MapRotationAssignmentOperationId == operationId)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (committed != null)
            {
                return new ApiResponse<MapRotationAssignmentOperationDto>(committed.ToDto()).ToApiResult(HttpStatusCode.Created);
            }

            var assignment = await context.MapRotationServerAssignments
                .FirstOrDefaultAsync(a => a.MapRotationServerAssignmentId == createDto.MapRotationServerAssignmentId, cancellationToken).ConfigureAwait(false);

            if (assignment == null)
            {
                return new ApiResult<MapRotationAssignmentOperationDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationAssignmentOperationDto>(new ApiError("ASSIGNMENT_NOT_FOUND", "The specified server assignment does not exist.")));
            }

            if (!Enum.IsDefined(createDto.OperationType)
                || (assignment.DeploymentState == (int)DeploymentState.Removed
                    && (createDto.OperationType != AssignmentOperationType.Deactivate || assignment.ActivationState == (int)ActivationState.Inactive))
                || (createDto.OperationType == AssignmentOperationType.Remove && assignment.ActivationState != (int)ActivationState.Inactive))
            {
                return new ApiResult<MapRotationAssignmentOperationDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationAssignmentOperationDto>(new ApiError("ASSIGNMENT_IN_USE", "The operation is not valid for this assignment.")));
            }

            var entity = createDto.ToEntity();
            entity.MapRotationAssignmentOperationId = operationId;
            context.MapRotationAssignmentOperations.Add(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var created = await context.MapRotationAssignmentOperations
                .AsNoTracking()
                .FirstAsync(op => op.MapRotationAssignmentOperationId == entity.MapRotationAssignmentOperationId, cancellationToken).ConfigureAwait(false);

            var result = created.ToDto();
            return new ApiResponse<MapRotationAssignmentOperationDto>(result).ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Updates an existing assignment operation.
        /// </summary>
        /// <param name="operationId">The unique identifier of the assignment operation to update.</param>
        /// <param name="status">The new status for the operation.</param>
        /// <param name="error">Optional error message.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the assignment operation was updated; otherwise, a 404 Not Found response.</returns>
        [HttpPatch("map-rotations/assignments/operations/{operationId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAssignmentOperation(Guid operationId, [FromQuery] AssignmentOperationStatus status, [FromQuery] string? error = null, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapRotationsApi)this).UpdateAssignmentOperation(operationId, status, error, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates an existing assignment operation.
        /// </summary>
        /// <param name="operationId">The unique identifier of the assignment operation to update.</param>
        /// <param name="status">The new status for the operation.</param>
        /// <param name="error">Optional error message.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the assignment operation was updated if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapRotationsApi.UpdateAssignmentOperation(Guid operationId, AssignmentOperationStatus status, string? error, CancellationToken cancellationToken)
            => await ExecuteMutationAsync(() => UpdateOperation(operationId, status, error, cancellationToken), cancellationToken).ConfigureAwait(false);

        private async Task<ApiResult> UpdateOperation(Guid operationId, AssignmentOperationStatus status, string? error, CancellationToken cancellationToken)
        {
            var entity = await context.MapRotationAssignmentOperations
                .FirstOrDefaultAsync(op => op.MapRotationAssignmentOperationId == operationId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            if (!Enum.IsDefined(status))
            {
                return InvalidAssignment("INVALID_OPERATION_STATUS", "Unknown operation status.");
            }

            if (entity.Status != (int)AssignmentOperationStatus.InProgress)
            {
                return entity.Status == (int)status
                    ? new ApiResponse().ToApiResult()
                    : InvalidAssignment("OPERATION_COMPLETED", "A terminal operation cannot change status.");
            }

            entity.Status = (int)status;
            entity.Error = error;

            if (status is AssignmentOperationStatus.Succeeded or AssignmentOperationStatus.Failed or AssignmentOperationStatus.Cancelled)
            {
                entity.CompletedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult();
        }

        // ───────────────────────── Private Helpers ─────────────────────────

        private async Task<ApiResult?> ValidateConfigTarget(MapRotationServerAssignment entity, UpdateMapRotationServerAssignmentDto update, bool targetChanged, CancellationToken cancellationToken)
        {
            if (!targetChanged)
            {
                return null;
            }

            if (!CanRemove(entity) || update.ActivationState is not (null or ActivationState.Inactive)
                || await context.MapRotationAssignmentOperations.AnyAsync(op =>
                    op.MapRotationServerAssignment.GameServerId == entity.GameServerId
                    && op.Status == (int)AssignmentOperationStatus.InProgress, cancellationToken).ConfigureAwait(false))
            {
                return InvalidAssignment("CONFIG_TARGET_IN_USE", "Deactivate the assignment and finish outstanding operations before editing its config target.");
            }

            return null;
        }

        private static ApiResult? ValidateStateValues(UpdateMapRotationServerAssignmentDto update)
        {
            if ((update.DeploymentState.HasValue && !Enum.IsDefined(update.DeploymentState.Value))
                || (update.ActivationState.HasValue && !Enum.IsDefined(update.ActivationState.Value)))
            {
                return InvalidAssignment("INVALID_ASSIGNMENT_STATE", "Unknown assignment state.");
            }

            return null;
        }

        private static ApiResult? ValidateDeploymentEvidence(MapRotationServerAssignment entity, UpdateMapRotationServerAssignmentDto update)
        {
            var version = entity.MapRotation.Version;
            var deployedVersion = update.DeployedVersion ?? entity.DeployedVersion;
            var activatedVersion = update.ActivatedVersion ?? entity.ActivatedVersion;
            var deployment = update.DeploymentState ?? (DeploymentState)entity.DeploymentState;
            if ((update.DeploymentState == DeploymentState.Synced && deployedVersion != version)
                || !IsValidVersion(update.DeployedVersion, version) || !IsValidVersion(update.ActivatedVersion, version))
            {
                return InvalidAssignment("ASSIGNMENT_NOT_CURRENT", "Deployment evidence must identify a valid rotation version; synced confirmation must identify the current version.");
            }

            if (update.ActivationState is ActivationState.Active or ActivationState.Activating
                && (deployment != DeploymentState.Synced || deployedVersion != version
                    || (update.ActivationState == ActivationState.Active && activatedVersion != version)))
            {
                return InvalidAssignment("ASSIGNMENT_NOT_CURRENT", "Activation requires the current rotation version to be synced; active confirmation requires its activated version.");
            }

            return null;
        }

        private static bool IsValidVersion(int? version, int currentVersion)
            => !version.HasValue || (version > 0 && version <= currentVersion);

        private static ApiResult? ValidateRemoval(MapRotationServerAssignment entity, UpdateMapRotationServerAssignmentDto update, bool targetChanged)
        {
            var deployment = update.DeploymentState ?? (DeploymentState)entity.DeploymentState;
            var activation = update.ActivationState ?? (ActivationState)entity.ActivationState;
            if (update.DeploymentState.HasValue && (deployment is DeploymentState.Removing or DeploymentState.Removed)
                && (activation != ActivationState.Inactive || !CanConfirmRemoval(entity)))
            {
                return InvalidAssignment("ASSIGNMENT_IN_USE", "Deactivate the assignment and finish other outstanding operations before confirming unassignment.");
            }

            if (entity.DeploymentState == (int)DeploymentState.Removed
                && (deployment != DeploymentState.Removed || targetChanged
                    || update.DeployedVersion.HasValue || update.ActivatedVersion.HasValue || update.UnassignedAt.HasValue))
            {
                return InvalidAssignment("ASSIGNMENT_REMOVED", "Removed assignments are retained history; create a new assignment instead.");
            }

            return null;
        }

        private async Task<bool> HasDuplicateConfigTarget(MapRotationServerAssignment entity, string? path, string? variable, DeploymentState deployment, CancellationToken cancellationToken)
        {
            if (deployment is DeploymentState.Removed or DeploymentState.Removing or DeploymentState.Failed)
            {
                return false;
            }

            return await context.MapRotationServerAssignments.AnyAsync(a =>
                a.MapRotationServerAssignmentId != entity.MapRotationServerAssignmentId
                && a.MapRotationId == entity.MapRotationId && a.GameServerId == entity.GameServerId
                && a.ConfigFilePath == path && a.ConfigVariableName == variable
                && a.DeploymentState != (int)DeploymentState.Removed
                && a.DeploymentState != (int)DeploymentState.Removing
                && a.DeploymentState != (int)DeploymentState.Failed, cancellationToken).ConfigureAwait(false);
        }

        private async Task ConfirmConfigTargetHandover(MapRotationServerAssignment entity, string? path, string? variable, CancellationToken cancellationToken)
        {
            var activeAssignments = await context.MapRotationServerAssignments.Where(a =>
                a.MapRotationServerAssignmentId != entity.MapRotationServerAssignmentId
                && a.GameServerId == entity.GameServerId && a.ConfigFilePath == path && a.ConfigVariableName == variable
                && a.ActivationState == (int)ActivationState.Active && a.DeploymentState != (int)DeploymentState.Removed)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            foreach (var active in activeAssignments)
            {
                active.ActivationState = (int)ActivationState.Inactive;
                active.ActivatedVersion = null;
                active.UpdatedAt = now;
            }
        }

        private static ApiResult InvalidAssignment(string code, string message)
            => new(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(code, message)));

        private sealed class AssignmentRemovalAttempt
        {
            public DateTime RemovedAt { get; } = DateTime.UtcNow;
            public bool Unassigned { get; set; }
            public bool Purged { get; set; }
        }

        private static bool CanRemove(MapRotationServerAssignment assignment)
            => assignment.ActivationState == (int)ActivationState.Inactive
                && assignment.DeploymentState != (int)DeploymentState.Syncing
                && !assignment.MapRotationAssignmentOperations.Any(op => op.Status == (int)AssignmentOperationStatus.InProgress);

        private static bool CanConfirmRemoval(MapRotationServerAssignment assignment)
        {
            if (assignment.ActivationState != (int)ActivationState.Inactive
                || assignment.DeploymentState == (int)DeploymentState.Syncing)
            {
                return false;
            }

            var inProgress = assignment.MapRotationAssignmentOperations
                .Where(op => op.Status == (int)AssignmentOperationStatus.InProgress).ToList();
            // The unassignment workflow confirms metadata before completing its own Remove operation.
            return inProgress.Count == 0 || (inProgress.Count == 1 && inProgress[0].OperationType == (int)AssignmentOperationType.Remove);
        }

        private static DateTime RetainUntil(MapRotationServerAssignment assignment)
        {
            var anchor = assignment.UnassignedAt ?? assignment.UpdatedAt;
            var latestCompletion = assignment.MapRotationAssignmentOperations
                .Select(op => op.CompletedAt ?? op.StartedAt).DefaultIfEmpty(anchor).Max();
            return (latestCompletion > anchor ? latestCompletion : anchor).AddDays(90);
        }

        private async Task<T> ExecuteMutationAsync<T>(Func<Task<T>> mutation, CancellationToken cancellationToken)
        {
            return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                if (!context.Database.IsRelational())
                {
                    return await mutation().ConfigureAwait(false);
                }

                context.ChangeTracker.Clear();
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
                var result = await mutation().ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }).ConfigureAwait(false);
        }

        private static IQueryable<MapRotation> ApplyFilters(IQueryable<MapRotation> query, GameType[]? gameTypes, string? gameMode, MapRotationStatus? status, string? filterString, Guid? createdByUserId, MapRotationsFilter? filter)
        {
            if (gameTypes?.Length > 0)
            {
                var gameTypeInts = gameTypes.Select(gt => gt.ToGameTypeInt()).ToArray();
                query = query.Where(mr => gameTypeInts.Contains(mr.GameType));
            }

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                query = query.Where(mr => mr.GameMode == gameMode);
            }

            if (status.HasValue)
            {
                var statusInt = (int)status.Value;
                query = query.Where(mr => mr.Status == statusInt);
            }

            if (!string.IsNullOrWhiteSpace(filterString))
            {
                query = query.Where(mr => mr.Title.Contains(filterString) || (mr.Description != null && mr.Description.Contains(filterString)));
            }

            if (createdByUserId.HasValue)
            {
                query = query.Where(mr => mr.CreatedByUserId == createdByUserId.Value);
            }

            return filter switch
            {
                MapRotationsFilter.HasActiveAssignments => query.Where(mr => mr.MapRotationServerAssignments.Any(a => a.DeploymentState != (int)DeploymentState.Removed)),
                MapRotationsFilter.HasStaleAssignments => query.Where(mr => mr.MapRotationServerAssignments.Any(a =>
                    a.DeploymentState == (int)DeploymentState.Synced && a.DeployedVersion != null && a.DeployedVersion < mr.Version)),
                _ => query
            };
        }

        private static IQueryable<MapRotation> ApplyOrderingAndPagination(IQueryable<MapRotation> query, int skipEntries, int takeEntries, MapRotationsOrder? order)
        {
            var orderedQuery = order switch
            {
                MapRotationsOrder.TitleAsc => query.OrderBy(mr => mr.Title),
                MapRotationsOrder.TitleDesc => query.OrderByDescending(mr => mr.Title),
                MapRotationsOrder.GameModeAsc => query.OrderBy(mr => mr.GameMode),
                MapRotationsOrder.GameModeDesc => query.OrderByDescending(mr => mr.GameMode),
                MapRotationsOrder.MapCountAsc => query.OrderBy(mr => mr.MapRotationMaps.Count),
                MapRotationsOrder.MapCountDesc => query.OrderByDescending(mr => mr.MapRotationMaps.Count),
                MapRotationsOrder.ServerCountAsc => query.OrderBy(mr => mr.MapRotationServerAssignments.Count),
                MapRotationsOrder.ServerCountDesc => query.OrderByDescending(mr => mr.MapRotationServerAssignments.Count),
                MapRotationsOrder.CreatedAtAsc => query.OrderBy(mr => mr.CreatedAt),
                MapRotationsOrder.CreatedAtDesc => query.OrderByDescending(mr => mr.CreatedAt),
                MapRotationsOrder.UpdatedAtAsc => query.OrderBy(mr => mr.UpdatedAt),
                MapRotationsOrder.UpdatedAtDesc => query.OrderByDescending(mr => mr.UpdatedAt),
                MapRotationsOrder.CreatedByAsc => query.OrderBy(mr => mr.CreatedByUser != null ? mr.CreatedByUser.DisplayName : ""),
                MapRotationsOrder.CreatedByDesc => query.OrderByDescending(mr => mr.CreatedByUser != null ? mr.CreatedByUser.DisplayName : ""),
                _ => query.OrderBy(mr => mr.Title)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }
    }
}
