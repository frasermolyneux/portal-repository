using System.Net;
using Asp.Versioning;
using Azure.Identity;
using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class MapsController : Controller, IMapsApi
    {
        private readonly PortalDbContext context;

        public MapsController(
            PortalDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
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
            var response = await ((IMapsApi)this).GetMap(mapId, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific map by game type and map name.
        /// </summary>
        /// <param name="gameType">The game type of the map to retrieve.</param>
        /// <param name="mapName">The name of the map to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The map details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("maps/{gameType:string}/{mapName}")]
        [ProducesResponseType<MapDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMap(GameType gameType, string mapName, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).GetMap(gameType, mapName, cancellationToken);

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
            var map = await context.Maps
                .Include(m => m.MapVotes)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MapId == mapId, cancellationToken);

            if (map == null)
                return new ApiResult<MapDto>(HttpStatusCode.NotFound);

            var result = map.ToDto();

            return new ApiResponse<MapDto>(result).ToApiResult();
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
            var map = await context.Maps
                .Include(m => m.MapVotes)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.GameType == gameType.ToGameTypeInt() && m.MapName == mapName, cancellationToken);

            if (map == null)
                return new ApiResult<MapDto>(HttpStatusCode.NotFound);

            var result = map.ToDto();

            return new ApiResponse<MapDto>(result).ToApiResult();
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
                skipEntries = 0;

            if (!takeEntries.HasValue)
                takeEntries = 20;

            string[]? mapNamesFilter = null;
            if (!string.IsNullOrWhiteSpace(mapNames))
            {
                var split = mapNames.Split(",");
                mapNamesFilter = split.Select(mn => mn.Trim()).ToArray();
            }

            var response = await ((IMapsApi)this).GetMaps(gameType, mapNamesFilter, filter, filterString, skipEntries.Value, takeEntries.Value, order, cancellationToken);

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
            var baseQuery = context.Maps.AsNoTracking().AsQueryable();

            // Calculate total count before applying filters
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Apply filters
            var filteredQuery = ApplyFilter(baseQuery, gameType, mapNames, filter, filterString);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken);

            // Apply ordering and pagination
            var orderedQuery = ApplyOrderAndLimits(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery.ToListAsync(cancellationToken);

            var entries = results.Select(m => m.ToDto()).ToList();

            var result = new CollectionModel<MapDto>(entries, totalCount, filteredCount);

            return new ApiResponse<CollectionModel<MapDto>>(result).ToApiResult();
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
            var response = await ((IMapsApi)this).CreateMap(createMapDto, cancellationToken);
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
            if (await context.Maps.AnyAsync(m => m.GameType == createMapDto.GameType.ToGameTypeInt() && m.MapName == createMapDto.MapName, cancellationToken))
            {
                var response = new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.MapConflictMessage));
                return response.ToApiResult(HttpStatusCode.Conflict);
            }

            var map = createMapDto.ToEntity();
            await context.Maps.AddAsync(map, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
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
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

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

            var response = await ((IMapsApi)this).CreateMaps(createMapDtos, cancellationToken);

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
            foreach (var createMapDto in createMapDtos)
            {
                if (await context.Maps.AnyAsync(m => m.GameType == createMapDto.GameType.ToGameTypeInt() && m.MapName == createMapDto.MapName, cancellationToken))
                {
                    var response = new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.MapConflictMessage));
                    return response.ToApiResult(HttpStatusCode.Conflict);
                }

                var map = createMapDto.ToEntity();
                await context.Maps.AddAsync(map, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Updates an existing map.
        /// </summary>
        /// <param name="editMapDto">The map data to update.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the map was updated; otherwise, appropriate error responses.</returns>
        [HttpPut("map")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMap([FromBody] EditMapDto editMapDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).UpdateMap(editMapDto, cancellationToken);
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
            var map = await context.Maps.FirstOrDefaultAsync(m => m.MapId == editMapDto.MapId, cancellationToken);
            if (map == null)
                return new ApiResult(HttpStatusCode.NotFound);

            editMapDto.ApplyTo(map);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Updates multiple maps in a single operation.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if all maps were updated; otherwise, appropriate error responses.</returns>
        [HttpPut("maps")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMaps(CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

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

            var response = await ((IMapsApi)this).UpdateMaps(editMapDtos, cancellationToken);

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
                var map = await context.Maps.FirstOrDefaultAsync(m => m.MapId == editMapDto.MapId, cancellationToken);
                if (map == null)
                    return new ApiResult(HttpStatusCode.NotFound);

                editMapDto.ApplyTo(map);
            }

            await context.SaveChangesAsync(cancellationToken);

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
            var response = await ((IMapsApi)this).DeleteMap(mapId, cancellationToken);

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
                .FirstOrDefaultAsync(m => m.MapId == mapId, cancellationToken);

            if (map == null)
                return new ApiResult(HttpStatusCode.NotFound);

            context.Remove(map);

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
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
            var response = await ((IMapsApi)this).RebuildMapPopularity(cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Rebuilds the popularity statistics for all maps based on vote data.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the map popularity was rebuilt.</returns>
        async Task<ApiResult> IMapsApi.RebuildMapPopularity(CancellationToken cancellationToken)
        {
            var maps = await context.Maps.Include(m => m.MapVotes).ToListAsync(cancellationToken);

            foreach (var map in maps)
            {
                map.TotalLikes = map.MapVotes.Count(mv => mv.Like);
                map.TotalDislikes = map.MapVotes.Count(mv => !mv.Like);
                map.TotalVotes = map.MapVotes.Count;

                if (map.TotalVotes > 0)
                {
                    map.LikePercentage = (double)map.TotalLikes / map.TotalVotes * 100;
                    map.DislikePercentage = (double)map.TotalDislikes / map.TotalVotes * 100;
                }
                else
                {
                    map.LikePercentage = 0;
                    map.DislikePercentage = 0;
                }
            }

            await context.SaveChangesAsync(cancellationToken);

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
        public async Task<IActionResult> UpsertMapVote([FromBody] UpsertMapVoteDto upsertMapVoteDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).UpsertMapVote(upsertMapVoteDto, cancellationToken);
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
            var mapVote = await context.MapVotes
                .FirstOrDefaultAsync(mv => mv.MapId == upsertMapVoteDto.MapId && mv.PlayerId == upsertMapVoteDto.PlayerId && mv.GameServerId == upsertMapVoteDto.GameServerId, cancellationToken);

            if (mapVote == null)
            {
                var newMapVote = upsertMapVoteDto.ToEntity();

                context.MapVotes.Add(newMapVote);
            }
            else
            {
                if (mapVote.Like != upsertMapVoteDto.Like)
                {
                    mapVote.Like = upsertMapVoteDto.Like;
                    mapVote.Timestamp = DateTime.UtcNow;
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Creates or updates multiple map votes in a single operation.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if all map votes were processed; otherwise, appropriate error responses.</returns>
        [HttpPost("maps/votes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpsertMapVotes(CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

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

            var response = await ((IMapsApi)this).UpsertMapVotes(upsertMapVoteDtos, cancellationToken);

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
            // Get all existing votes for the maps/players/servers in the request
            var mapIds = upsertMapVoteDtos.Select(x => x.MapId).ToList();
            var playerIds = upsertMapVoteDtos.Select(x => x.PlayerId).ToList();
            var gameServerIds = upsertMapVoteDtos.Select(x => x.GameServerId).ToList();

            var existingVotes = await context.MapVotes
                .Where(mv => mapIds.Contains(mv.MapId) && playerIds.Contains(mv.PlayerId) &&
                            (mv.GameServerId == null || gameServerIds.Contains(mv.GameServerId.Value)))
                .ToListAsync(cancellationToken);

            foreach (var upsertMapVote in upsertMapVoteDtos)
            {
                var mapVote = existingVotes
                    .FirstOrDefault(mv => mv.MapId == upsertMapVote.MapId && mv.PlayerId == upsertMapVote.PlayerId && mv.GameServerId == upsertMapVote.GameServerId);

                if (mapVote == null)
                {
                    var newMapVote = upsertMapVote.ToEntity();

                    context.MapVotes.Add(newMapVote);
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

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
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
                await file.CopyToAsync(stream, cancellationToken);
            }

            var response = await ((IMapsApi)this).UpdateMapImage(mapId, filePath, cancellationToken);

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
            var map = await context.Maps
                .FirstOrDefaultAsync(m => m.MapId == mapId, cancellationToken);

            if (map == null)
                return new ApiResult(HttpStatusCode.NotFound);

            var blobEndpoint = Environment.GetEnvironmentVariable("appdata_storage_blob_endpoint");
            if (string.IsNullOrEmpty(blobEndpoint))
                return new ApiResult(HttpStatusCode.InternalServerError);

            var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient("map-images");

            var blobKey = $"{map.GameType.ToGameType()}_{map.MapName}.jpg";
            var blobClient = containerClient.GetBlobClient(blobKey);
            if (await blobClient.ExistsAsync(cancellationToken))
            {
                await blobClient.DeleteAsync(cancellationToken: cancellationToken);
            }

            await blobClient.UploadAsync(filePath, cancellationToken);

            map.MapImageUri = blobClient.Uri.ToString();

            await context.SaveChangesAsync(cancellationToken);

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
            if (gameType.HasValue)
                query = query.Where(m => m.GameType == gameType.Value.ToGameTypeInt()).AsQueryable();

            if (mapNames != null && mapNames.Length > 0)
                query = query.Where(m => mapNames.Contains(m.MapName)).AsQueryable();

            if (filterString is not null && filterString.Length > 0)
            {
                query = query.Where(m => m.MapName.Contains(filterString));
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
    }
}

