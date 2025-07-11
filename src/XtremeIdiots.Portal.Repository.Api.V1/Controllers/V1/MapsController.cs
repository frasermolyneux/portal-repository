using System.Net;
using Asp.Versioning;
using AutoMapper;
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

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class MapsController : Controller, IMapsApi
    {
        private readonly PortalDbContext context;
        private readonly IMapper mapper;

        public MapsController(
            PortalDbContext context,
            IMapper mapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [Route("maps/{mapId}")]
        public async Task<IActionResult> GetMap(Guid mapId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).GetMap(mapId, cancellationToken);

            return response.ToHttpResult();
        }

        [HttpGet]
        [Route("maps/{gameType}/{mapName}")]
        public async Task<IActionResult> GetMap(GameType gameType, string mapName, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).GetMap(gameType, mapName, cancellationToken);

            return response.ToHttpResult();
        }

        async Task<ApiResult<MapDto>> IMapsApi.GetMap(Guid mapId, CancellationToken cancellationToken)
        {
            var map = await context.Maps
                .Include(m => m.MapVotes)
                .SingleOrDefaultAsync(m => m.MapId == mapId, cancellationToken);

            if (map == null)
                return new ApiResult<MapDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<MapDto>(map);

            return new ApiResponse<MapDto>(result).ToApiResult();
        }

        async Task<ApiResult<MapDto>> IMapsApi.GetMap(GameType gameType, string mapName, CancellationToken cancellationToken)
        {
            var map = await context.Maps
                .Include(m => m.MapVotes)
                .SingleOrDefaultAsync(m => m.GameType == gameType.ToGameTypeInt() && m.MapName == mapName, cancellationToken);

            if (map == null)
                return new ApiResult<MapDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<MapDto>(map);

            return new ApiResponse<MapDto>(result).ToApiResult();
        }

        [HttpGet]
        [Route("maps")]
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

        async Task<ApiResult<CollectionModel<MapDto>>> IMapsApi.GetMaps(GameType? gameType, string[]? mapNames, MapsFilter? filter, string? filterString, int skipEntries, int takeEntries, MapsOrder? order, CancellationToken cancellationToken)
        {
            var query = context.Maps.AsQueryable();
            query = ApplyFilter(query, gameType, null, null, null);
            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplyFilter(query, gameType, mapNames, filter, filterString);
            var filteredCount = await query.CountAsync(cancellationToken);

            query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
            var results = await query.ToListAsync(cancellationToken);

            var entries = results.Select(m => mapper.Map<MapDto>(m)).ToList();

            var result = new CollectionModel<MapDto>(entries, totalCount, filteredCount);

            return new ApiResponse<CollectionModel<MapDto>>(result).ToApiResult();
        }

        async Task<ApiResult> IMapsApi.CreateMap(CreateMapDto createMapDto, CancellationToken cancellationToken)
        {
            if (await context.Maps.AnyAsync(m => m.GameType == createMapDto.GameType.ToGameTypeInt() && m.MapName == createMapDto.MapName, cancellationToken))
            {
                var response = new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.MapConflictMessage));
                return response.ToApiResult(HttpStatusCode.Conflict);
            }

            var map = mapper.Map<Map>(createMapDto);
            await context.Maps.AddAsync(map, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("maps")]
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

        async Task<ApiResult> IMapsApi.CreateMaps(List<CreateMapDto> createMapDtos, CancellationToken cancellationToken)
        {
            foreach (var createMapDto in createMapDtos)
            {
                if (await context.Maps.AnyAsync(m => m.GameType == createMapDto.GameType.ToGameTypeInt() && m.MapName == createMapDto.MapName, cancellationToken))
                {
                    var response = new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.MapConflictMessage));
                    return response.ToApiResult(HttpStatusCode.Conflict);
                }

                var map = mapper.Map<Map>(createMapDto);
                await context.Maps.AddAsync(map, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK);
        }

        async Task<ApiResult> IMapsApi.UpdateMap(EditMapDto editMapDto, CancellationToken cancellationToken)
        {
            var map = await context.Maps.SingleOrDefaultAsync(m => m.MapId == editMapDto.MapId, cancellationToken);
            if (map == null)
                return new ApiResult(HttpStatusCode.NotFound);

            mapper.Map(editMapDto, map);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpPut]
        [Route("maps")]
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

        async Task<ApiResult> IMapsApi.UpdateMaps(List<EditMapDto> editMapDtos, CancellationToken cancellationToken)
        {
            foreach (var editMapDto in editMapDtos)
            {
                var map = await context.Maps.SingleAsync(m => m.MapId == editMapDto.MapId);
                mapper.Map(editMapDto, map);
            }

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpDelete]
        [Route("maps/{mapId}")]
        public async Task<IActionResult> DeleteMap(Guid mapId, CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).DeleteMap(mapId, cancellationToken);

            return response.ToHttpResult();
        }

        async Task<ApiResult> IMapsApi.DeleteMap(Guid mapId, CancellationToken cancellationToken)
        {
            var map = await context.Maps
                .SingleOrDefaultAsync(m => m.MapId == mapId, cancellationToken);

            if (map == null)
                return new ApiResult(HttpStatusCode.NotFound);

            context.Remove(map);

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("maps/popularity")]
        public async Task<IActionResult> RebuildMapPopularity(CancellationToken cancellationToken = default)
        {
            var response = await ((IMapsApi)this).RebuildMapPopularity(cancellationToken);

            return response.ToHttpResult();
        }

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

            return new ApiResult(HttpStatusCode.OK);
        }

        async Task<ApiResult> IMapsApi.UpsertMapVote(UpsertMapVoteDto upsertMapVoteDto, CancellationToken cancellationToken)
        {
            var mapVote = await context.MapVotes.SingleOrDefaultAsync(mv => mv.MapId == upsertMapVoteDto.MapId && mv.PlayerId == upsertMapVoteDto.PlayerId && mv.GameServerId == upsertMapVoteDto.GameServerId, cancellationToken);

            if (mapVote == null)
            {
                var newMapVote = mapper.Map<MapVote>(upsertMapVoteDto);
                newMapVote.Timestamp = DateTime.UtcNow;

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

            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("maps/votes")]
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

        async Task<ApiResult> IMapsApi.UpsertMapVotes(List<UpsertMapVoteDto> upsertMapVoteDtos, CancellationToken cancellationToken)
        {
            foreach (var upsertMapVote in upsertMapVoteDtos)
            {
                var mapVote = await context.MapVotes.SingleOrDefaultAsync(mv => mv.MapId == upsertMapVote.MapId && mv.PlayerId == upsertMapVote.PlayerId && mv.GameServerId == upsertMapVote.GameServerId, cancellationToken);

                if (mapVote == null)
                {
                    var newMapVote = mapper.Map<MapVote>(upsertMapVote);
                    newMapVote.Timestamp = DateTime.UtcNow;

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

            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("maps/{mapId}/image")]
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

        async Task<ApiResult> IMapsApi.UpdateMapImage(Guid mapId, string filePath, CancellationToken cancellationToken)
        {
            var map = await context.Maps
                .SingleOrDefaultAsync(m => m.MapId == mapId, cancellationToken);

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

            return new ApiResult(HttpStatusCode.OK);
        }

        private IQueryable<Map> ApplyFilter(IQueryable<Map> query, GameType? gameType, string[]? mapNames, MapsFilter? filter, string? filterString)
        {
            if (gameType.HasValue)
                query = query.Where(m => m.GameType == gameType.Value.ToGameTypeInt()).AsQueryable();

            if (mapNames != null && mapNames.Length > 0)
                query = query.Where(m => mapNames.Contains(m.MapName)).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filterString))
            {
                query = query.Where(m => m.MapName.Contains(filterString!)).AsQueryable();
            }

            switch (filter)
            {
                case MapsFilter.EmptyMapImage:
                    query = query.Where(m => m.MapImageUri == null).AsQueryable();
                    break;
            }

            return query;
        }

        private IQueryable<Map> ApplyOrderAndLimits(IQueryable<Map> query, int skipEntries, int takeEntries, MapsOrder? order)
        {
            switch (order)
            {
                case MapsOrder.MapNameAsc:
                    query = query.OrderBy(m => m.MapName).AsQueryable();
                    break;
                case MapsOrder.MapNameDesc:
                    query = query.OrderByDescending(m => m.MapName).AsQueryable();
                    break;
                case MapsOrder.GameTypeAsc:
                    query = query.OrderBy(m => m.GameType).AsQueryable();
                    break;
                case MapsOrder.GameTypeDesc:
                    query = query.OrderByDescending(m => m.GameType).AsQueryable();
                    break;
                case MapsOrder.PopularityAsc:
                    query = query.OrderByDescending(m => m.TotalLikes).AsQueryable();
                    break;
                case MapsOrder.PopularityDesc:
                    query = query.OrderByDescending(m => m.TotalDislikes).AsQueryable();
                    break;
            }

            query = query.Skip(skipEntries).AsQueryable();
            query = query.Take(takeEntries).AsQueryable();

            return query;
        }
    }
}

