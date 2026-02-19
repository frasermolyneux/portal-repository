using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeMapPacksApi : IMapPacksApi
{
    private readonly ConcurrentDictionary<Guid, MapPackDto> _mapPacks = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);

    public FakeMapPacksApi AddMapPack(MapPackDto mapPack) { _mapPacks[mapPack.MapPackId] = mapPack; return this; }
    public FakeMapPacksApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeMapPacksApi Reset() { _mapPacks.Clear(); _errorResponses.Clear(); return this; }

    public Task<ApiResult<MapPackDto>> GetMapPack(Guid mapPackId, CancellationToken cancellationToken = default)
    {
        if (_mapPacks.TryGetValue(mapPackId, out var mp))
            return Task.FromResult(new ApiResult<MapPackDto>(HttpStatusCode.OK, new ApiResponse<MapPackDto>(mp)));
        return Task.FromResult(new ApiResult<MapPackDto>(HttpStatusCode.NotFound, new ApiResponse<MapPackDto>(new ApiError("NOT_FOUND", "Map pack not found"))));
    }

    public Task<ApiResult<CollectionModel<MapPackDto>>> GetMapPacks(GameType[]? gameTypes, Guid[]? gameServerIds, MapPacksFilter? filter, int skipEntries, int takeEntries, MapPacksOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _mapPacks.Values.AsEnumerable();
        if (gameServerIds != null) items = items.Where(mp => gameServerIds.Contains(mp.GameServerId));
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapPackDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<MapPackDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapPackDto>>(collection)));
    }

    public Task<ApiResult> CreateMapPack(CreateMapPackDto createMapPackDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CreateMapPacks(List<CreateMapPackDto> createMapPackDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateMapPack(UpdateMapPackDto updateMapPackDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteMapPack(Guid mapPackId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
