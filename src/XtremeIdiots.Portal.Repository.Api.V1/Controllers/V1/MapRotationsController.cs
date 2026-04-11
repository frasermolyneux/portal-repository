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
                return new ApiResult<MapRotationDto>(HttpStatusCode.NotFound);

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
                        return new ApiResponse(new ApiError("INVALID_GAME_TYPE", $"Invalid game type value: '{gt.Trim()}'."))
                            .ToBadRequestResult()
                            .ToHttpResult();
                    parsed.Add(gameType);
                }
                gameTypesFilter = parsed.ToArray();
            }

            var response = await ((IMapRotationsApi)this).GetMapRotations(gameTypesFilter, gameMode, status, filterString, filter, skipEntries, takeEntries, order, cancellationToken).ConfigureAwait(false);
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
            return await ((IMapRotationsApi)this).GetMapRotations(gameTypes, gameMode, null, null, filter, skipEntries, takeEntries, order, cancellationToken).ConfigureAwait(false);
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
            var baseQuery = context.MapRotations.AsNoTracking();

            var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var filteredQuery = ApplyFilters(baseQuery, gameTypes, gameMode, status, filterString, filter);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var orderedQuery = ApplyOrderingAndPagination(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery
                .Include(mr => mr.MapRotationMaps)
                .Include(mr => mr.MapRotationServerAssignments)
                .Include(mr => mr.CreatedByUser)
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
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();

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
                    return new ApiResult<MapRotationDto>(HttpStatusCode.BadRequest,
                        new ApiResponse<MapRotationDto>(new ApiError("INVALID_MAP_IDS", "One or more map IDs do not exist.")));

                var gameTypeInt = (int)createMapRotationDto.GameType;
                if (maps.Any(m => m.GameType != gameTypeInt))
                    return new ApiResult<MapRotationDto>(HttpStatusCode.BadRequest,
                        new ApiResponse<MapRotationDto>(new ApiError("MAP_GAME_TYPE_MISMATCH", "One or more maps do not match the rotation's game type.")));

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
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();

            if (updateMapRotationDto.MapRotationId != mapRotationId)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestEntityMismatch, ApiErrorMessages.RequestEntityMismatchMessage))).ToHttpResult();

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
        {
            var entity = await context.MapRotations
                .Include(mr => mr.MapRotationMaps)
                .FirstOrDefaultAsync(mr => mr.MapRotationId == updateMapRotationDto.MapRotationId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
                return new ApiResult(HttpStatusCode.NotFound);

            updateMapRotationDto.ApplyTo(entity);

            if (updateMapRotationDto.MapIds != null)
            {
                var distinctMapIds = updateMapRotationDto.MapIds.Distinct().ToList();
                var maps = await context.Maps
                    .AsNoTracking()
                    .Where(m => distinctMapIds.Contains(m.MapId))
                    .ToListAsync(cancellationToken).ConfigureAwait(false);

                if (maps.Count != distinctMapIds.Count)
                    return new ApiResult(HttpStatusCode.BadRequest,
                        new ApiResponse(new ApiError("INVALID_MAP_IDS", "One or more map IDs do not exist.")));

                if (maps.Any(m => m.GameType != entity.GameType))
                    return new ApiResult(HttpStatusCode.BadRequest,
                        new ApiResponse(new ApiError("MAP_GAME_TYPE_MISMATCH", "One or more maps do not match the rotation's game type.")));

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
                    entity.Version++;
                }
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
            var entity = await context.MapRotations
                .Include(mr => mr.MapRotationMaps)
                .Include(mr => mr.MapRotationServerAssignments)
                    .ThenInclude(a => a.MapRotationAssignmentOperations)
                .FirstOrDefaultAsync(mr => mr.MapRotationId == mapRotationId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
                return new ApiResult(HttpStatusCode.NotFound);

            // Block delete if any assignments are actively deployed (allow Removed and Failed)
            if (entity.MapRotationServerAssignments.Any(a => a.DeploymentState != (int)DeploymentState.Removed && a.DeploymentState != (int)DeploymentState.Failed))
                return new ApiResult(HttpStatusCode.BadRequest,
                    new ApiResponse(new ApiError("HAS_ACTIVE_ASSIGNMENTS", "Cannot delete a map rotation that has active server assignments. Unassign all servers first.")));

            // Remove children explicitly to avoid FK violations
            foreach (var assignment in entity.MapRotationServerAssignments)
                context.MapRotationAssignmentOperations.RemoveRange(assignment.MapRotationAssignmentOperations);

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
                return new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.NotFound);

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
                query = query.Where(a => a.MapRotationId == mapRotationId.Value);

            if (gameServerId.HasValue)
                query = query.Where(a => a.GameServerId == gameServerId.Value);

            if (deploymentState.HasValue)
                query = query.Where(a => a.DeploymentState == (int)deploymentState.Value);

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
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();

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
            var mapRotation = await context.MapRotations
                .AsNoTracking()
                .FirstOrDefaultAsync(mr => mr.MapRotationId == createDto.MapRotationId, cancellationToken).ConfigureAwait(false);

            if (mapRotation == null)
                return new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("MAP_ROTATION_NOT_FOUND", "The specified map rotation does not exist.")));

            var gameServer = await context.GameServers
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.GameServerId == createDto.GameServerId, cancellationToken).ConfigureAwait(false);

            if (gameServer == null)
                return new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("GAME_SERVER_NOT_FOUND", "The specified game server does not exist.")));

            if (mapRotation.GameType != gameServer.GameType)
                return new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("GAME_TYPE_MISMATCH", "The map rotation game type does not match the game server game type.")));

            var duplicateExists = await context.MapRotationServerAssignments
                .AnyAsync(a =>
                    a.GameServerId == createDto.GameServerId &&
                    a.ConfigFilePath == createDto.ConfigFilePath &&
                    a.ConfigVariableName == createDto.ConfigVariableName &&
                    a.DeploymentState != (int)DeploymentState.Removed,
                    cancellationToken).ConfigureAwait(false);

            if (duplicateExists)
                return new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("DUPLICATE_CONFIG_TARGET", "An active assignment already exists for this game server with the same config file path and variable name.")));

            var entity = createDto.ToEntity();
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
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();

            if (updateDto.MapRotationServerAssignmentId != assignmentId)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestEntityMismatch, ApiErrorMessages.RequestEntityMismatchMessage))).ToHttpResult();

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
            var entity = await context.MapRotationServerAssignments
                .FirstOrDefaultAsync(a => a.MapRotationServerAssignmentId == updateDto.MapRotationServerAssignmentId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
                return new ApiResult(HttpStatusCode.NotFound);

            updateDto.ApplyTo(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Deletes a server assignment by setting its deployment state to Removing.
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
        /// Deletes a server assignment by setting its deployment state to Removing.
        /// </summary>
        /// <param name="assignmentId">The unique identifier of the server assignment to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the server assignment was marked for removal if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IMapRotationsApi.DeleteServerAssignment(Guid assignmentId, CancellationToken cancellationToken)
        {
            var entity = await context.MapRotationServerAssignments
                .FirstOrDefaultAsync(a => a.MapRotationServerAssignmentId == assignmentId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
                return new ApiResult(HttpStatusCode.NotFound);

            entity.DeploymentState = (int)DeploymentState.Removing;
            entity.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();

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
            var assignmentExists = await context.MapRotationServerAssignments
                .AnyAsync(a => a.MapRotationServerAssignmentId == createDto.MapRotationServerAssignmentId, cancellationToken).ConfigureAwait(false);

            if (!assignmentExists)
                return new ApiResult<MapRotationAssignmentOperationDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<MapRotationAssignmentOperationDto>(new ApiError("ASSIGNMENT_NOT_FOUND", "The specified server assignment does not exist.")));

            var entity = createDto.ToEntity();
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
        {
            var entity = await context.MapRotationAssignmentOperations
                .FirstOrDefaultAsync(op => op.MapRotationAssignmentOperationId == operationId, cancellationToken).ConfigureAwait(false);

            if (entity == null)
                return new ApiResult(HttpStatusCode.NotFound);

            entity.Status = (int)status;
            entity.Error = error;

            if (status is AssignmentOperationStatus.Succeeded or AssignmentOperationStatus.Failed or AssignmentOperationStatus.Cancelled)
                entity.CompletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult();
        }

        // ───────────────────────── Private Helpers ─────────────────────────

        private static IQueryable<MapRotation> ApplyFilters(IQueryable<MapRotation> query, GameType[]? gameTypes, string? gameMode, MapRotationStatus? status, string? filterString, MapRotationsFilter? filter)
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
                _ => query.OrderBy(mr => mr.Title)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }
    }
}
