using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeMapsApi : IMapsApi
{
    private readonly ConcurrentDictionary<Guid, MapDto> _maps = new();
    private readonly ConcurrentDictionary<Guid, MapVoteDto> _mapVotes = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);

    public FakeMapsApi AddMap(MapDto map) { _maps[map.MapId] = map; return this; }
    public FakeMapsApi AddMapVote(MapVoteDto mapVote)
    {
        // Auto-resolve Map navigation from _maps store if not already set
        if (mapVote.Map is null && _maps.TryGetValue(mapVote.MapId, out var map))
            mapVote = mapVote with { Map = map };

        _mapVotes[mapVote.MapVoteId] = mapVote;
        return this;
    }
    public FakeMapsApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeMapsApi Reset() { _maps.Clear(); _mapVotes.Clear(); _errorResponses.Clear(); return this; }

    public Task<ApiResult<MapDto>> GetMap(Guid mapId, CancellationToken cancellationToken = default)
    {
        if (_maps.TryGetValue(mapId, out var map))
            return Task.FromResult(new ApiResult<MapDto>(HttpStatusCode.OK, new ApiResponse<MapDto>(map)));
        return Task.FromResult(new ApiResult<MapDto>(HttpStatusCode.NotFound, new ApiResponse<MapDto>(new ApiError("NOT_FOUND", "Map not found"))));
    }

    public Task<ApiResult<MapDto>> GetMap(GameType gameType, string mapName, CancellationToken cancellationToken = default)
    {
        var map = _maps.Values.FirstOrDefault(m => m.GameType == gameType && m.MapName == mapName);
        if (map != null)
            return Task.FromResult(new ApiResult<MapDto>(HttpStatusCode.OK, new ApiResponse<MapDto>(map)));
        return Task.FromResult(new ApiResult<MapDto>(HttpStatusCode.NotFound, new ApiResponse<MapDto>(new ApiError("NOT_FOUND", "Map not found"))));
    }

    public Task<ApiResult<CollectionModel<MapDto>>> GetMaps(GameType? gameType, string[]? mapNames, MapsFilter? filter, string? filterString, int skipEntries, int takeEntries, MapsOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _maps.Values.AsEnumerable();
        if (gameType.HasValue) items = items.Where(m => m.GameType == gameType.Value);
        if (mapNames != null) items = items.Where(m => mapNames.Contains(m.MapName));
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<MapDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<MapVoteDto>>> GetMapVotes(GameType? gameType, Guid? mapId, int skipEntries, int takeEntries, MapVotesOrder? order, CancellationToken cancellationToken = default)
    {
        // Resolve Map navigation from _maps store at query time to avoid order-of-insertion issues
        var items = _mapVotes.Values.Select(mv =>
            mv.Map is null && _maps.TryGetValue(mv.MapId, out var map) ? mv with { Map = map } : mv);

        if (gameType.HasValue) items = items.Where(mv => mv.Map?.GameType == gameType.Value);
        if (mapId.HasValue) items = items.Where(mv => mv.MapId == mapId.Value);

        var totalCount = _mapVotes.Count;
        var filteredCount = items.Count();

        items = order switch
        {
            MapVotesOrder.TimestampAsc => items.OrderBy(mv => mv.Timestamp).ThenBy(mv => mv.MapVoteId),
            MapVotesOrder.MapNameAsc => items.OrderBy(mv => mv.Map?.MapName).ThenBy(mv => mv.MapVoteId),
            MapVotesOrder.MapNameDesc => items.OrderByDescending(mv => mv.Map?.MapName).ThenBy(mv => mv.MapVoteId),
            _ => items.OrderByDescending(mv => mv.Timestamp).ThenBy(mv => mv.MapVoteId)
        };

        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapVoteDto> { Items = list };
        var response = new ApiResponse<CollectionModel<MapVoteDto>>(collection)
        {
            Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
        };
        return Task.FromResult(new ApiResult<CollectionModel<MapVoteDto>>(HttpStatusCode.OK, response));
    }

    public Task<ApiResult> CreateMap(CreateMapDto createMapDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CreateMaps(List<CreateMapDto> createMapDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateMap(EditMapDto editMapDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateMaps(List<EditMapDto> editMapDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteMap(Guid mapId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> RebuildMapPopularity(CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpsertMapVote(UpsertMapVoteDto upsertMapVoteDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpsertMapVotes(List<UpsertMapVoteDto> upsertMapVoteDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateMapImage(Guid mapId, string filePath, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> ClearMapImage(Guid mapId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
